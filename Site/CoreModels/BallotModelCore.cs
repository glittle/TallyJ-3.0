using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public abstract class BallotModelCore : DataConnectedModel, IBallotModel
  {
    protected BallotModelCore()
    {
      _helper = new BallotHelper();
    }

    private BallotAnalyzer _analyzer;
    private readonly BallotHelper _helper;

    //    private VoteHelper _voteHelper;

    private BallotAnalyzer BallotAnalyzerLocal => _analyzer ?? (_analyzer = new BallotAnalyzer());

    //    protected VoteHelper VoteHelperLocal
    //    {
    //      get { return _voteHelper ?? (_voteHelper = new VoteHelper(true)); }
    //    }

    #region IBallotModel Members

    public object SwitchToBallotAndGetInfo(int ballotId, bool refresh)
    {
      SetAsCurrentBallot(ballotId);

      var ballot = GetCurrentBallot(refresh);
      if (ballot == null)
      {
        return new { };
      }

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

    public abstract JsonResult StartNewBallotJson();

    /// <Summary>Delete a ballot, but only if already empty</Summary>
    public JsonResult DeleteBallotJson()
    {
      Ballot ballot;
      try
      {
        ballot = CurrentRawBallot();
      }
      catch (Exception e)
      {
        if (e.Message == "Sequence contains no matching element")
        {
          return new
          {
            Deleted = false,
            Message = "Ballot not found"
          }.AsJsonResult();
        }
        throw;
      }
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

      var ballotStatusInfo = BallotAnalyzerLocal.UpdateBallotStatus(ballot, VoteInfosFor(ballot), true, true);

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
      var ballot = GetCurrentBallot();
      if (ballot == null)
      {
        return null;
      }

      var allVotes = new VoteCacher(Db).AllForThisElection;
      return new
      {
        Ballot = BallotInfoForJs(ballot, allVotes),
        Votes = CurrentVotesForJs(ballot, allVotes),
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
          name = vi.PersonFullName,
          changed = vi.PersonCombinedInfo.HasContent() && !vi.PersonCombinedInfo.StartsWith(vi.PersonCombinedInfoInVote ?? "NULL"),
          invalid = vi.VoteIneligibleReasonGuid,
          ineligible = vi.PersonIneligibleReasonGuid,
          onlineRawVote = vi.OnlineVoteRaw,
          //ineligible = VoteHelperLocal.IneligibleToReceiveVotes(vi.PersonIneligibleReasonGuid, vi.PersonCanReceiveVotes)
        });
    }

    public JsonResult SaveVote(int personId, int voteId, Guid? invalidReason, int lastVid, int count, bool verifying)
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }
      var locationModel = new LocationModel();
      if (locationModel.HasMultiplePhysicalLocations && UserSession.CurrentLocation == null)
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
      var personCacher = new PersonCacher(Db);

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
          // problem... client is focused on a different ballot!
          return new { Updated = false, Error = "Invalid vote/ballot id" }.AsJsonResult();
        }

        Db.Vote.Attach(vote);

        vote.SingleNameElectionCount = count;

        var person1 = personCacher.AllForThisElection.SingleOrDefault(p => p.C_RowId == personId);
        vote.PersonCombinedInfo = person1?.CombinedInfo;

        if (UserSession.CurrentLocation.IsVirtual)
        {
          // changing person on an online ballot

          if (person1 == null)
          {
            vote.PersonGuid = null;
          }
          else
          {
            vote.PersonGuid = person1.PersonGuid;
            invalidReason = person1.IneligibleReasonGuid;
          }

          vote.StatusCode = invalidReason == null ? VoteStatusCode.Ok : VoteStatusCode.Spoiled;

        }

        DetermineInvalidReasonGuid(invalidReason, vote);

        vote.StatusCode =
          VoteAnalyzer.DetermineStatus(new VoteInfo(vote, UserSession.CurrentElection, ballot,
            UserSession.CurrentLocation, person1));

        Db.SaveChanges();

        var votes = voteCacher.UpdateItemAndSaveCache(vote).AllForThisElection;

        var ballotStatusInfo = BallotAnalyzerLocal.UpdateBallotStatus(ballot, VoteInfosFor(ballot, votes), true);
        var sum = _helper.BallotCount(ballot.LocationGuid, isSingleName, null, votes);

        ballot.Teller1 = UserSession.GetCurrentTeller(1);
        ballot.Teller2 = UserSession.GetCurrentTeller(2);

        new BallotCacher(Db).UpdateItemAndSaveCache(ballot);
        Db.SaveChanges();

        var ballotCounts = isSingleName ? new VoteCacher().AllForThisElection
          .Where(v => v.BallotGuid == ballot.BallotGuid)
          .Sum(v => v.SingleNameElectionCount) : 0;
        var ballotCountNames = isSingleName ? new VoteCacher().AllForThisElection
          .Count(v => v.BallotGuid == ballot.BallotGuid) : 0;

        return new
        {
          Updated = true,
          BallotStatus = ballotStatusInfo.Status.Value,
          BallotStatusText = ballotStatusInfo.Status.DisplayText,
          ballotStatusInfo.SpoiledCount,
          LocationBallotsEntered = sum,
          BallotId = ballot.C_RowId,
          SingleBallotCount = ballotCounts,
          SingleBallotNames = ballotCountNames,
          VoteUpdates = GetVoteUpdates(lastVid, voteCacher, isSingleName, personCacher),
          LastVid = vote.C_RowId,
          vote.InvalidReasonGuid,
          Name = person1?.C_FullName,
          person1?.Area,
          vote = CurrentVotesForJs(GetCurrentBallot(), new List<Vote> { vote }).First()
        }.AsJsonResult();
      }

      // make a new Vote record
      var location = new LocationCacher(Db).AllForThisElection.Single(l => l.LocationGuid == ballot.LocationGuid);

      if (location.IsVirtual)
      {
        return new { Updated = false, Error = "Cannot add votes to an online ballot" }.AsJsonResult();
      }

      var invalidReasonGuid = DetermineInvalidReasonGuid(invalidReason);

      var person = personCacher.AllForThisElection.SingleOrDefault(p => p.C_RowId == personId);

      var ok = person != null || (invalidReason != null && invalidReasonGuid != Guid.Empty);

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
          StatusCode = VoteStatusCode.Ok,
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
        Db.SaveChanges();

        var ballotCounts = isSingleName ? new VoteCacher().AllForThisElection
          .Where(v => v.BallotGuid == ballot.BallotGuid)
          .Sum(v => v.SingleNameElectionCount) : 0;
        var ballotCountNames = isSingleName ? new VoteCacher().AllForThisElection
          .Count(v => v.BallotGuid == ballot.BallotGuid) : 0;

        return new
        {
          Updated = true,
          VoteId = vote.C_RowId,
          pos = vote.PositionOnBallot,
          BallotStatus = ballotStatusInfo.Status.Value,
          BallotStatusText = ballotStatusInfo.Status.DisplayText,
          ballotStatusInfo.SpoiledCount,
          BallotId = ballot.C_RowId,
          LocationBallotsEntered = sum,
          SingleBallotCount = ballotCounts,
          SingleBallotNames = ballotCountNames,
          VoteUpdates = GetVoteUpdates(lastVid, voteCacher, isSingleName, personCacher),
          LastVid = vote.C_RowId

        }.AsJsonResult();
      }

      // don't recognize person id
      return new { Updated = false, Error = "Invalid person. Please try again." }.AsJsonResult();
    }

    private object GetVoteUpdates(int lastVoteId, VoteCacher voteCacher, bool isSingleName, PersonCacher personCacher)
    {
      if (lastVoteId == 0)
      {
        // single name elections
        return null;
      }

      // ignores vote and ballot status - count how many times the name has been written on ballots

      var peopleInRecentVotes = voteCacher
        .AllForThisElection
        .Where(v => v.C_RowId > lastVoteId && v.PersonGuid != null)
        .Select(v => v.PersonGuid)
        .Distinct();

      var counts = voteCacher
        .AllForThisElection
        .Where(v => peopleInRecentVotes.Contains(v.PersonGuid))
        .GroupBy(v => v.PersonGuid)
        .Join(personCacher.AllForThisElection, votes => votes.Key, p => p.PersonGuid, (votes, p) => new { votes, p })
        .Select(g => new
        {
          Id = g.p.C_RowId,
          Count = g.votes.Sum(v => isSingleName ? v.SingleNameElectionCount : 1)
        })
        .ToList();

      return counts;
    }

    /// <summary>
    /// This works only for the person deleting the vote. Other tellers will not know that their count of votes for this person
    /// should be reduced. However, when another vote is made for this person, all tellers will eventually know about it.
    /// The counts returned are not guaranteed as one teller's computer may get out of sync with the real total. Refreshing the
    /// teller's page should fix it.
    /// </summary>
    /// <param name="personGuid"></param>
    /// <param name="voteCacher"></param>
    /// <param name="isSingleName"></param>
    /// <returns></returns>
    private object GetVoteUpdatesOnDelete(Guid? personGuid, VoteCacher voteCacher, bool isSingleName)
    {
      var counts = voteCacher.AllForThisElection.Where(v => v.PersonGuid == personGuid)
        .GroupBy(v => v.PersonGuid)
        .Select(g => new
        {
          PersonGuid = g.Key,
          Count = g.Sum(v => isSingleName ? v.SingleNameElectionCount : 1).DefaultTo(0)
        })
        .ToList();

      if (!counts.Any(v => v.PersonGuid == personGuid))
      {
        counts.Add(new { PersonGuid = personGuid, Count = 0 });
      }

      return counts;
    }

    public JsonResult DeleteVote(int vid)
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }

      VoteCacher voteCacher = new VoteCacher(Db);

      var vote = voteCacher.AllForThisElection.SingleOrDefault(v => v.C_RowId == vid);
      if (vote == null)
      {
        return new { Message = "Not found" }.AsJsonResult();
      }

      var ballot = CurrentRawBallot();
      var isSingleName = UserSession.CurrentElection.IsSingleNameElection;
      var location = new LocationCacher(Db).AllForThisElection.Single(l => l.LocationGuid == ballot.LocationGuid);

      if (location.IsVirtual)
      {
        return new { Message = "Cannot delete votes from an online ballot." }.AsJsonResult();
      }

      Db.Vote.Attach(vote);
      Db.Vote.Remove(vote);
      Db.SaveChanges();

      var allVotes = voteCacher.RemoveItemAndSaveCache(vote).AllForThisElection;

      var ballotStatusInfo = BallotAnalyzerLocal.UpdateBallotStatus(ballot, VoteInfosFor(ballot, allVotes), false);

      UpdateVotePositions(vote.BallotGuid, allVotes);

      var sum = _helper.BallotCount(location.LocationGuid, isSingleName, null, allVotes);

      new BallotCacher(Db).UpdateItemAndSaveCache(ballot);
      Db.SaveChanges();

      var ballotCounts = isSingleName ? new VoteCacher().AllForThisElection
        .Where(v => v.BallotGuid == ballot.BallotGuid)
        .Sum(v => v.SingleNameElectionCount) : 0;
      var ballotCountNames = isSingleName ? new VoteCacher().AllForThisElection
        .Count(v => v.BallotGuid == ballot.BallotGuid) : 0;

      return new
      {
        Deleted = true,
        Votes = CurrentVotesForJs(GetCurrentBallot(), allVotes),
        BallotStatus = ballotStatusInfo.Status.Value,
        BallotStatusText = ballotStatusInfo.Status.DisplayText,
        ballotStatusInfo.SpoiledCount,
        LocationBallotsEntered = sum,
        BallotId = ballot.C_RowId,
        SingleBallotCount = ballotCounts,
        SingleBallotNames = ballotCountNames,
        VoteUpdates = GetVoteUpdatesOnDelete(vote.PersonGuid, voteCacher, isSingleName),
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



    /// <Summary>Get the current Ballot. Only use when there is a ballot.</Summary>
    private Ballot CurrentRawBallot()
    {
      var ballotId = SessionKey.CurrentBallotId.FromSession(0);
      return new BallotCacher(Db).AllForThisElection.Single(b => b.C_RowId == ballotId);
      //      return Db.Ballot.Single(b => b.C_RowId == ballotId);
    }


    /// <Summary>Current Ballot... could be null</Summary>
    public Ballot GetCurrentBallot(bool refresh = false)
    {
      var isSingleNameElection = UserSession.CurrentElection.IsSingleNameElection;
      var currentBallotId = SessionKey.CurrentBallotId.FromSession(0);

      var ballotCacher = new BallotCacher(Db);

      var ballot = ballotCacher.GetById(currentBallotId);

      if (ballot == null && isSingleNameElection)
      {
        ballot = ballotCacher.GetByComputerCode();
      }

      if (ballot == null)
      {
        if (isSingleNameElection)
        {
          // will create empty ballot for this computer... do we need it?
          //        ballot = CreateAndRegisterBallot();
        }
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

      if (ballot != null && ballot.C_RowId != currentBallotId)
      {
        SessionKey.CurrentBallotId.SetInSession(ballot.C_RowId);
      }

      return ballot;
    }

    #endregion

    public abstract object BallotInfoForJs(Ballot b, List<Vote> allVotes);

    public bool CreateBallotForOnlineVoter(List<OnlineRawVote> poolList, out string errorMessage)
    {
      //{Id: 0, First:"", Last:"", OtherInfo:""}

      // double check
      var numberToElect = UserSession.CurrentElection.NumberToElect;
      if (poolList.Count != numberToElect)
      {
        errorMessage = $"Invalid number of votes ({poolList.Count}). Need {numberToElect}.";
        return false;
      }

      var ballotCacher = new BallotCacher(Db);
      var voteCacher = new VoteCacher(Db);
      var locationHelper = new LocationModel(Db);
      var location = locationHelper.GetOnlineLocation();

      // create ballot
      var ballot = new Ballot
      {
        BallotGuid = Guid.NewGuid(),
        LocationGuid = location.LocationGuid,
        ComputerCode = ComputerModel.ComputerCodeForOnline,
        BallotNumAtComputer = 0, // maxNum + 1, // will reset later
        StatusCode = BallotStatusEnum.Empty,
      };
      Db.Ballot.Add(ballot);
      Db.SaveChanges();

      ballotCacher.UpdateItemAndSaveCache(ballot);

      // add Votes
      var nextVoteNum = 0;

      foreach (var rawVote in poolList)
      {
        Vote vote;
        if (rawVote.Id > 0)
        {
          var person = new PersonCacher(Db).AllForThisElection.FirstOrDefault(b => b.C_RowId == rawVote.Id);
          if (person == null)
          {
            errorMessage = $"Error converting pool id {rawVote.Id} to person.";
            return false;
          }

          vote = new Vote
          {
            BallotGuid = ballot.BallotGuid,
            PositionOnBallot = ++nextVoteNum,
            StatusCode = VoteStatusCode.Ok,
            PersonGuid = person.PersonGuid,
            PersonCombinedInfo = person.CombinedInfo,
            SingleNameElectionCount = 1, // okay if set for normal election too
            InvalidReasonGuid = person.CanReceiveVotes.AsBoolean(true) ? null : person.IneligibleReasonGuid
          };
        }
        else
        {
          // "random" vote
          vote = new Vote
          {
            BallotGuid = ballot.BallotGuid,
            PositionOnBallot = ++nextVoteNum,
            StatusCode = VoteStatusCode.OnlineRaw,
            SingleNameElectionCount = 1,
            OnlineVoteRaw = JsonConvert.SerializeObject(rawVote),
          };

          // attempt to match if it is exact...
          var matched = new PersonCacher(Db).AllForThisElection
            // match on first and last name only
            .Where(p => p.FirstName.ToLower() == rawVote.First.ToLower() && p.LastName.ToLower() == rawVote.Last.ToLower())
            // don't match if our list has "otherInfo" for this person - there might be some special considerations
            .Where(p => p.OtherInfo.HasNoContent())
            .ToList();

          if (matched.Count == 1)
          {
            // found one exact match
            var person = matched[0];
            vote.StatusCode = VoteStatusCode.Ok;
            vote.PersonGuid = person.PersonGuid;
            vote.PersonCombinedInfo = person.CombinedInfo;
            vote.InvalidReasonGuid = person.CanReceiveVotes.AsBoolean(true) ? null : person.IneligibleReasonGuid;
          }
        }

        Db.Vote.Add(vote);

        Db.SaveChanges();

        voteCacher.UpdateItemAndSaveCache(vote);
      }

      var votes = voteCacher.AllForThisElection;
      BallotAnalyzerLocal.UpdateBallotStatus(ballot, VoteInfosFor(ballot, votes), true);

      ballotCacher.UpdateItemAndSaveCache(ballot);
      Db.SaveChanges();

      errorMessage = "";
      return true;
    }


    // public List<Vote> CurrentVotes()
    // {
    //   var ballot = GetCurrentBallot();
    //
    //   if (ballot == null)
    //   {
    //     return new List<Vote>();
    //   }
    //   return new VoteCacher(Db).AllForThisElection.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();
    // }

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

    protected Ballot CreateAndRegisterBallot()
    {
      var currentLocationGuid = UserSession.CurrentLocationGuid;
      if (!currentLocationGuid.HasContent())
      {
        var locationModel = new LocationModel();
        currentLocationGuid = locationModel.GetLocations_Physical().First().LocationGuid;
        UserSession.CurrentLocationGuid = currentLocationGuid;
      }
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

    public abstract object CurrentBallotsInfoList(bool refresh = false);

  }
}