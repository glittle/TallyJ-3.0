using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public abstract class BallotModelCore : DataConnectedModel, IBallotModel
  {
    public const string ReasonGroupIneligible = "Ineligible";

    public BallotModelCore() {
      _helper = new BallotHelper();
    }

    private BallotAnalyzer _analyzer;
    private BallotHelper _helper;

    //    private VoteHelper _voteHelper;

    protected BallotAnalyzer BallotAnalyzerLocal
    {
      get { return _analyzer ?? (_analyzer = new BallotAnalyzer()); }
    }

//    protected VoteHelper VoteHelperLocal
//    {
//      get { return _voteHelper ?? (_voteHelper = new VoteHelper(true)); }
//    }

    #region IBallotModel Members

    public object SwitchToBallotAndGetInfo(int ballotId, bool refresh)
    {
      SetAsCurrentBallot(ballotId);

      var ballot = GetCurrentBallot(refresh);

      SessionKey.CurrentLocationGuid.SetInSession(ballot.LocationGuid);

      var allVotes = new VoteCacher(Db).AllForThisElection;
      return new
      {

        BallotInfo = new
        {
          Ballot = BallotInfoForJs(ballot, allVotes),
          Votes = CurrentVotesForJs(ballot, allVotes),
          NumNeeded = UserSession.CurrentElection.NumberToElect
        },
        Location = ContextItems.LocationModel.LocationInfoForJson(UserSession.CurrentLocation)
      };
    }

    public bool SortVotes(List<int> ids, VoteCacher voteCacher)
    {
      
      var ballotGuid = CurrentRawBallot().BallotGuid;

      var allVotes = voteCacher.AllForThisElection;

      var votes = allVotes.Where(v => v.BallotGuid == ballotGuid);

      var position = 1;
      foreach (var vote in ids.Select(id => votes.SingleOrDefault(v => v.C_RowId == id)).Where(vote => vote != null))
      {
        Db.Vote.Attach(vote);
        vote.PositionOnBallot = position;
        position++;
      }
      Db.SaveChanges();

      voteCacher.ReplaceEntireCache(allVotes);

      return true;
    }

    public JsonResult StartNewBallotJson()
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }
      var locationModel = new LocationModel();
      if (locationModel.HasLocations && UserSession.CurrentLocation == null)
      {
        return new { Message = "Must select your location first!" }.AsJsonResult();
      }
      if (UserSession.GetCurrentTeller(1).HasNoContent())
      {
        return new { Message = "Must select \"Teller at Keyboard\" first!" }.AsJsonResult();
      }

      var ballotInfo = CreateAndRegisterBallot();

      var allVotes = new VoteCacher(Db).AllForThisElection;
      return new
      {
        BallotInfo = new
        {
          Ballot = BallotInfoForJs(ballotInfo, allVotes),
          Votes = CurrentVotesForJs(ballotInfo, allVotes),
          NumNeeded = UserSession.CurrentElection.NumberToElect
        },
        Ballots = CurrentBallotsInfoList()
      }.AsJsonResult();
    }

    /// <Summary>Delete a ballot, but only if already empty</Summary>
    public JsonResult DeleteBallotJson()
    {
      var ballot = CurrentRawBallot();
      var ballotGuid = ballot.BallotGuid;

      var hasVotes = new VoteCacher(Db).AllForThisElection.Any(v => v.BallotGuid == ballotGuid);

      if (hasVotes)
      {
        return new
        {
          Deleted = false,
          Message = "Can only delete a ballot when it has no votes."
        }.AsJsonResult();
      }

      new BallotCacher(Db).RemoveItemAndSaveCache(ballot);

      Db.Ballot.Attach(ballot);
      Db.Ballot.Remove(ballot);
      Db.SaveChanges();

      return new
      {
        Deleted = true,
        Ballots = CurrentBallotsInfoList(),
        Location = ContextItems.LocationModel.LocationInfoForJson(UserSession.CurrentLocation)
      }.AsJsonResult();
    }

    public JsonResult SetNeedsReview(bool needsReview)
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }

      var ballot = CurrentRawBallot();

      Db.Ballot.Attach(ballot);

      ballot.StatusCode = needsReview ? BallotStatusEnum.Review : BallotStatusEnum.Ok;

      var ballotStatusInfo = BallotAnalyzerLocal.UpdateBallotStatus(ballot, VoteInfosFor(ballot), true);

      Db.SaveChanges();

      new BallotCacher(Db).UpdateItemAndSaveCache(ballot);

      return new
      {
        BallotStatus = ballotStatusInfo.Status.Value,
        BallotStatusText = ballotStatusInfo.Status.DisplayText,
        ballotStatusInfo.SpoiledCount
      }.AsJsonResult();
    }

    /// <Summary>Switch current ballot</Summary>
    public void SetAsCurrentBallot(int ballotId)
    {
      if (ballotId == SessionKey.CurrentBallotId.FromSession(0))
      {
        return;
      }

      // learn about the one wanted
      var ballot = new BallotCacher(Db).AllForThisElection.SingleOrDefault(b => b.C_RowId == ballotId);

      if (ballot == null)
      {
        // invalid request!?
        return;
      }

      //var wantedComputerCode = ballotInfo.ComputerCode;
      //if (wantedComputerCode != UserSession.CurrentComputerCode)
      //{
      //  // get our record, update it, and save it back
      //  var computer = UserSession.CurrentComputer;
      //  Db.Computer.Attach(computer);

      //  computer.ComputerCode = wantedComputerCode;
      //  computer.LastContact = DateTime.Now;
      //  Db.SaveChanges();

      //  SessionKey.CurrentComputer.SetInSession(computer);
      //}

      SessionKey.CurrentBallotId.SetInSession(ballot.C_RowId);
    }

    public abstract int NextBallotNumAtComputer();

    public object CurrentBallotInfo()
    {
      var ballotInfo = GetCurrentBallot();
      if (ballotInfo == null)
      {
        return null;
      }

      var allVotes = new VoteCacher(Db).AllForThisElection;
      return new
      {
        Ballot = BallotInfoForJs(ballotInfo, allVotes),
        Votes = CurrentVotesForJs(ballotInfo, allVotes),
        NumNeeded = UserSession.CurrentElection.NumberToElect
      };
    }

    public IEnumerable<object> CurrentVotesForJs(Ballot ballotInfo, List<Vote> allVotes)
    {
      return VoteInfosFor(ballotInfo, allVotes)
        .OrderBy(v => v.PositionOnBallot)
        .Select(vi => new
        {
          vid = vi.VoteId,
          count = vi.SingleNameElectionCount,
          pid = vi.PersonId,
          pos = vi.PositionOnBallot,
          name = vi.PersonFullNameFL,
          changed = !Equals(vi.PersonCombinedInfo, vi.PersonCombinedInfoInVote),
          invalid = vi.VoteIneligibleReasonGuid,
          ineligible = vi.PersonIneligibleReasonGuid,
          //ineligible = VoteHelperLocal.IneligibleToReceiveVotes(vi.PersonIneligibleReasonGuid, vi.PersonCanReceiveVotes)
        });
    }

    public JsonResult SaveVote(int personId, int voteId, int count, Guid? invalidReason)
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }
      var locationModel = new LocationModel();
      if (locationModel.HasLocations && UserSession.CurrentLocation == null)
      {
        return new { Message = "Must select your location first!" }.AsJsonResult();
      }
      if (UserSession.GetCurrentTeller(1).HasNoContent())
      {
        return new { Message = "Must select \"Teller at Keyboard\" first!" }.AsJsonResult();
      }

      var isSingleName = UserSession.CurrentElection.IsSingleNameElection;

      var ballot = GetCurrentBallot();
      if (ballot == null)
      {
        // don't have an active Ballot!
        return new { Updated = false, Error = "Invalid ballot" }.AsJsonResult();
      }

      Db.Ballot.Attach(ballot);

      var voteCacher = new VoteCacher(Db);

      if (voteId != 0)
      {
        // update existing record

        // find info about the existing Vote
        var vote = voteCacher.AllForThisElection.SingleOrDefault(v => v.C_RowId == voteId);

        if (vote == null)
        {
          // problem... client has a vote number, but we didn't find...
          return new { Updated = false, Error = "Invalid vote id" }.AsJsonResult();
        }
        if (vote.BallotGuid != ballot.BallotGuid)
        {
          // problem... client is focused on a differnt ballot!
          return new { Updated = false, Error = "Invalid vote/ballot id" }.AsJsonResult();
        }

        var person1 = new PersonCacher(Db).AllForThisElection.SingleOrDefault(p => p.C_RowId == personId);

        Db.Vote.Attach(vote);

        vote.SingleNameElectionCount = count;
        vote.PersonCombinedInfo = person1 == null ? null : person1.CombinedInfo;

        DetermineInvalidReasonGuid(invalidReason, vote);

        vote.StatusCode =
          VoteAnalyzer.DetermineStatus(new VoteInfo(vote, UserSession.CurrentElection, ballot,
            UserSession.CurrentLocation, person1));

        Db.SaveChanges();



        var votes = voteCacher.UpdateItemAndSaveCache(vote).AllForThisElection;

        var ballotStatusInfo = BallotAnalyzerLocal.UpdateBallotStatus(ballot, VoteInfosFor(ballot, votes), true);
        var sum = _helper.BallotCount(ballot.LocationGuid, isSingleName, null, votes);

        new BallotCacher(Db).UpdateItemAndSaveCache(ballot);

        return new
        {
          Updated = true,
          BallotStatus = ballotStatusInfo.Status.Value,
          BallotStatusText = ballotStatusInfo.Status.DisplayText,
          ballotStatusInfo.SpoiledCount,
          LocationBallotsEntered = sum
        }.AsJsonResult();
      }

      // make a new Vote record

      var invalidReasonGuid = DetermineInvalidReasonGuid(invalidReason);

      var person = new PersonCacher(Db).AllForThisElection.SingleOrDefault(p => p.C_RowId == personId);

      var ok = person != null || invalidReasonGuid != Guid.Empty;

      if (ok)
      {
        var nextVoteNum = 1 + voteCacher.AllForThisElection.Where(v => v.BallotGuid == ballot.BallotGuid)
          .OrderByDescending(v => v.PositionOnBallot)
          .Take(1)
          .Select(b => b.PositionOnBallot)
          .SingleOrDefault();

        var vote = new Vote
        {
          BallotGuid = ballot.BallotGuid,
          PositionOnBallot = nextVoteNum,
          StatusCode = VoteHelper.VoteStatusCode.Ok,
          SingleNameElectionCount = count
        };
        if (person != null)
        {
          vote.PersonGuid = person.PersonGuid;
          vote.PersonCombinedInfo = person.CombinedInfo;
          vote.InvalidReasonGuid = person.CanReceiveVotes.AsBoolean(true) ? null : person.IneligibleReasonGuid;
          //          VoteHelperLocal.IneligibleToReceiveVotes(person.IneligibleReasonGuid,
          //            person.CanReceiveVotes);
        }
        vote.InvalidReasonGuid = invalidReasonGuid;
        Db.Vote.Add(vote);
        Db.SaveChanges();

        var votes = voteCacher.UpdateItemAndSaveCache(vote).AllForThisElection;

        var rawBallot = CurrentRawBallot();
        var ballotStatusInfo = BallotAnalyzerLocal.UpdateBallotStatus(rawBallot, VoteInfosFor(rawBallot, votes), true);

        var sum = _helper.BallotCount(ballot.LocationGuid, isSingleName, null, votes);

        new BallotCacher(Db).UpdateItemAndSaveCache(rawBallot);

        return new
        {
          Updated = true,
          VoteId = vote.C_RowId,
          pos = vote.PositionOnBallot,
          BallotStatus = ballotStatusInfo.Status.Value,
          BallotStatusText = ballotStatusInfo.Status.DisplayText,
          ballotStatusInfo.SpoiledCount,
          LocationBallotsEntered = sum
        }.AsJsonResult();
      }

      // don't recognize person id
      return new { Updated = false, Error = "Invalid person" }.AsJsonResult();
    }

    public JsonResult DeleteVote(int vid)
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }

      var vote = new VoteCacher(Db).AllForThisElection.SingleOrDefault(v => v.C_RowId == vid);
      if (vote == null)
      {
        return new { Message = "Not found" }.AsJsonResult();
      }

      Db.Vote.Attach(vote);
      Db.Vote.Remove(vote);
      Db.SaveChanges();

      var allVotes = new VoteCacher(Db).RemoveItemAndSaveCache(vote).AllForThisElection;

      UpdateVotePositions(vote.BallotGuid, allVotes);

      var ballot = CurrentRawBallot();
      var ballotStatusInfo = BallotAnalyzerLocal.UpdateBallotStatus(ballot, VoteInfosFor(ballot, allVotes), false);
      var isSingleName = UserSession.CurrentElection.IsSingleNameElection;
      var location = new LocationCacher(Db).AllForThisElection.Single(l => l.LocationGuid == ballot.LocationGuid);

      var sum = _helper.BallotCount(location.LocationGuid, isSingleName, null, allVotes);

      new BallotCacher(Db).UpdateItemAndSaveCache(ballot);

      return new
      {
        Deleted = true,
        Votes = CurrentVotesForJs(GetCurrentBallot(), allVotes),
        BallotStatus = ballotStatusInfo.Status.Value,
        BallotStatusText = ballotStatusInfo.Status.DisplayText,
        ballotStatusInfo.SpoiledCount,
        LocationBallotsEntered = sum
      }.AsJsonResult();
    }



    //    public string InvalidReasonsByIdJsonString()
    //    {
    //      return IneligibleReasonEnum.Items
    //        .Select(r => new
    //        {
    //          Id = r.IndexNum,
    //          r.Group,
    //          Desc = r.Description,
    //          r.CanVote,
    //          r.CanReceiveVotes
    //        })
    //        .SerializedAsJsonString();
    //
    //      //return Db.Reasons
    //      //  //.Where(r => r.ReasonGroup != ReasonGroupIneligible)
    //      //  .OrderByDescending(r => r.ReasonGroup) // put Ineligible at the bottom
    //      //  .ThenBy(r => r.SortOrder)
    //      //  .Select(r => new
    //      //                 {
    //      //                   Id = r.C_RowId,
    //      //                   Group = r.ReasonGroup + (r.ReasonGroup == ReasonGroupIneligible ? " (and not in list)" : ""),
    //      //                   Desc = r.ReasonDescription
    //      //                 })
    //      //  .SerializedAsJsonString();
    //    }

    //    public string InvalidReasonsByGuidJsonString()
    //    {
    //      return IneligibleReasonEnum.Items
    //        .Select(r => new
    //        {
    //          Guid = r.Value,
    //          r.Group,
    //          Desc = r.Description,
    //          r.CanVote,
    //          r.CanReceiveVotes
    //        })
    //        .SerializedAsJsonString();
    //    }

    public object CurrentBallotsInfoList(bool refresh = false)
    {
      if (refresh)
      {
        new ElectionAnalyzerNormal().RefreshBallotStatuses(); // identical for single name elections
        Db.SaveChanges();
      }

      var filter = UserSession.CurrentBallotFilter;
      var ballots = new BallotCacher(Db).AllForThisElection
        .Where(b => b.LocationGuid == UserSession.CurrentLocationGuid)
        .ToList()
        .Where(b => filter.HasNoContent() || filter == b.ComputerCode)
        .OrderBy(b => b.ComputerCode)
        .ThenBy(b => b.BallotNumAtComputer)
        .ToList();

      var totalCount = filter.HasNoContent()
        ? ballots.Count
        : new BallotCacher(Db).AllForThisElection.Count(b => b.LocationGuid == UserSession.CurrentLocationGuid);

      return BallotsInfoList(ballots, totalCount);
    }

    /// <Summary>Get the current Ballot. Only use when there is a ballot.</Summary>
    public Ballot CurrentRawBallot()
    {
      var ballotId = SessionKey.CurrentBallotId.FromSession(0);
      return new BallotCacher(Db).AllForThisElection.Single(b => b.C_RowId == ballotId);
      //      return Db.Ballot.Single(b => b.C_RowId == ballotId);
    }


    /// <Summary>Current Ballot... could be null</Summary>
    public Ballot GetCurrentBallot(bool refresh = false)
    {
      var createIfNeeded = UserSession.CurrentElection.IsSingleNameElection;
      var currentBallotId = SessionKey.CurrentBallotId.FromSession(0);

      var ballotCacher = new BallotCacher(Db);

      var ballot = ballotCacher.GetById(currentBallotId);

      if (ballot == null && createIfNeeded)
      {
        ballot = CreateAndRegisterBallot();
      }
      else
      {
        if (refresh)
        {
          Db.Ballot.Attach(ballot);
          var voteCacher = new VoteCacher(Db); 
          var votes = voteCacher.AllForThisElection;
          var voteInfos = VoteInfosFor(ballot, votes);

          SortVotes(voteInfos.OrderBy(vi => vi.PositionOnBallot).Select(v => v.VoteId).ToList(), voteCacher);
          voteInfos = VoteInfosFor(ballot, votes);

          BallotAnalyzerLocal.UpdateBallotStatus(ballot, voteInfos, true);
          ballotCacher.UpdateItemAndSaveCache(ballot);
          Db.SaveChanges();
        }
      }

      return ballot;
    }

    #endregion

    public abstract object BallotInfoForJs(Ballot b, List<Vote> allVotes);

    public List<Vote> CurrentVotes()
    {
      var ballot = GetCurrentBallot();

      if (ballot == null)
      {
        return new List<Vote>();
      }
      return new VoteCacher(Db).AllForThisElection.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();
    }

    public List<VoteInfo> VoteInfosFor(Ballot ballot, List<Vote> allVotes = null)
    {
      if (ballot == null)
      {
        return new List<VoteInfo>();
      }

      return _helper.VoteInfosForBallot(ballot, allVotes);
    }


    /// <Summary>Convert int to Guid for InvalidReason. If vote is given, assign if different</Summary>
    private Guid? DetermineInvalidReasonGuid(Guid? invalidReasonGuid, Vote vote = null)
    {
      if (vote != null && vote.InvalidReasonGuid != invalidReasonGuid)
      {
        vote.InvalidReasonGuid = invalidReasonGuid;
      }

      return invalidReasonGuid;
    }

    private void UpdateVotePositions(Guid ballotGuid, IEnumerable<Vote> allVotes)
    {
      var votes = allVotes
        .Where(v => v.BallotGuid == ballotGuid)
        .OrderBy(v => v.PositionOnBallot)
        .ToList();

      var position = 1;
      votes.ForEach(v => v.PositionOnBallot = position++);

      Db.SaveChanges();
    }

    public Ballot CreateAndRegisterBallot()
    {
      var currentLocationGuid = UserSession.CurrentLocationGuid;
      var computerCode = UserSession.CurrentComputerCode;

      var ballotCacher = new BallotCacher(Db);
      var firstBallot = ballotCacher.AllForThisElection.Any();

      var ballot = new Ballot
      {
        BallotGuid = Guid.NewGuid(),
        LocationGuid = currentLocationGuid,
        ComputerCode = computerCode,
        BallotNumAtComputer = NextBallotNumAtComputer(),
        StatusCode = BallotStatusEnum.Empty,
        Teller1 = UserSession.GetCurrentTeller(1),
        Teller2 = UserSession.GetCurrentTeller(2)
      };
      Db.Ballot.Add(ballot);

      if (firstBallot)
      {
        var locationCacher = new LocationCacher(Db);
        var location = locationCacher.AllForThisElection.FirstOrDefault(l => l.LocationGuid == currentLocationGuid);
        if (location != null && location.BallotsCollected.AsInt() == 0)
        {
          var ballotSources = new PersonCacher(Db)
            .AllForThisElection
            .Count(p => !string.IsNullOrEmpty(p.VotingMethod) && p.VotingLocationGuid == currentLocationGuid);
          location.BallotsCollected = ballotSources;
          locationCacher.UpdateItemAndSaveCache(location);
        }
      }

      Db.SaveChanges();

      ballotCacher.UpdateItemAndSaveCache(ballot);

      SessionKey.CurrentBallotId.SetInSession(ballot.C_RowId);

      return ballot; //TODO: used to be view
    }

    //    public string NewBallotsJsonString(long lastRowVersion)
    //    {
    //      var ballots = new BallotCacher(Db).AllForThisElection
    //        .Where(b => b.RowVersionInt > lastRowVersion)
    //        .ToList();
    //
    //      return ballots.Any()
    //        ? BallotsInfoList(ballots).SerializedAsJsonString()
    //        : "";
    //
    //      //todo...
    //    }

    private object BallotsInfoList(List<Ballot> ballots, int totalCount)
    {
      var maxRowVersion = ballots.Count == 0 ? 0 : ballots.Max(b => b.RowVersionInt);

      return new
      {
        Ballots = ballots.ToList().Select(ballot => BallotInfoForJs(ballot, new VoteCacher(Db).AllForThisElection)),
        Last = maxRowVersion,
        Total = totalCount
      };
    }
  }
}