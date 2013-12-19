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

    private BallotAnalyzer _analyzer;
    private VoteHelper _voteHelper;

    protected BallotAnalyzer Analyzer
    {
      get { return _analyzer ?? (_analyzer = new BallotAnalyzer()); }
    }

    protected VoteHelper VoteHelperLocal
    {
      get { return _voteHelper ?? (_voteHelper = new VoteHelper(true)); }
    }

    #region IBallotModel Members

    public object SwitchToBallotAndGetInfo(int ballotId, bool refresh)
    {
      SetAsCurrentBallot(ballotId);

      var ballotInfo = GetCurrentBallot(refresh);

      SessionKey.CurrentLocationGuid.SetInSession(ballotInfo.LocationGuid);

      return new
      {

        BallotInfo = new
        {
          Ballot = BallotInfoForJs(ballotInfo),
          Votes = CurrentVotesForJs(),
          NumNeeded = UserSession.CurrentElection.NumberToElect
        },
        Location = ContextItems.LocationModel.LocationInfoForJson(UserSession.CurrentLocation)
      };
    }

    public bool SortVotes(List<int> ids)
    {
      var voteCacher = new VoteCacher();
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
      var ballotInfo = CreateAndRegisterBallot();

      return new
      {
        BallotInfo = new
        {
          Ballot = BallotInfoForJs(ballotInfo),
          Votes = CurrentVotesForJs(),
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

      var hasVotes = new VoteCacher().AllForThisElection.Any(v => v.BallotGuid == ballotGuid);

      if (hasVotes)
      {
        return new
        {
          Deleted = false,
          Message = "Can only delete a ballot when it has no votes."
        }.AsJsonResult();
      }

      new BallotCacher().RemoveItemAndSaveCache(ballot);

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
      var ballot = CurrentRawBallot();

      Db.Ballot.Attach(ballot);

      ballot.StatusCode = needsReview ? BallotStatusEnum.Review : BallotStatusEnum.Ok;

      var ballotAnalyzer = new BallotAnalyzer();
      var ballotStatusInfo = ballotAnalyzer.UpdateBallotStatus(ballot, VoteInfosFor(ballot), true);

      Db.SaveChanges();

      new BallotCacher().UpdateItemAndSaveCache(ballot);

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
      var ballotInfo = new BallotCacher().AllForThisElection.SingleOrDefault(b => b.C_RowId == ballotId);

      if (ballotInfo == null)
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

      SessionKey.CurrentBallotId.SetInSession(ballotInfo.C_RowId);
    }

    public abstract int NextBallotNumAtComputer();

    public object CurrentBallotInfo()
    {
      var ballotInfo = GetCurrentBallot();
      if (ballotInfo == null)
      {
        return null;
      }

      return new
      {
        Ballot = BallotInfoForJs(ballotInfo),
        Votes = CurrentVotesForJs(),
        NumNeeded = UserSession.CurrentElection.NumberToElect
      };
    }

    public IEnumerable<object> CurrentVotesForJs()
    {
      return VoteInfosFor(GetCurrentBallot())
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
          ineligible = VoteHelperLocal.IneligibleToReceiveVotes(vi.PersonIneligibleReasonGuid, vi.CanReceiveVotes)
        });
    }

    public JsonResult SaveVote(int personId, int voteId, int count, Guid? invalidReason)
    {
      var isSingleName = UserSession.CurrentElection.IsSingleNameElection;

      var ballot = GetCurrentBallot();
      if (ballot == null)
      {
        // don't have an active Ballot!
        return new { Updated = false, Error = "Invalid ballot" }.AsJsonResult();
      }

      Db.Ballot.Attach(ballot);

      if (voteId != 0)
      {
        // update existing record

        // find info about the existing Vote
        var vote = new VoteCacher().AllForThisElection.SingleOrDefault(v => v.C_RowId == voteId);

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

        var person1 = new PersonCacher().AllForThisElection.SingleOrDefault(p => p.C_RowId == personId);

        Db.Vote.Attach(vote);

        vote.SingleNameElectionCount = count;
        vote.PersonCombinedInfo = person1 == null ? null : person1.CombinedInfo;

        DetermineInvalidReasonGuid(invalidReason, vote);

        Db.SaveChanges();



        new VoteCacher().UpdateItemAndSaveCache(vote);

        var ballotAnalyzer = new BallotAnalyzer();
        var ballotStatusInfo = ballotAnalyzer.UpdateBallotStatus(ballot, VoteInfosFor(ballot), true);
        var sum = BallotCount(ballot.LocationGuid, isSingleName);

        new BallotCacher().UpdateItemAndSaveCache(ballot);
        
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

      var person = new PersonCacher().AllForThisElection.SingleOrDefault(p => p.C_RowId == personId);

      var ok = person != null || invalidReasonGuid != Guid.Empty;

      if (ok)
      {
        var nextVoteNum = 1 + new VoteCacher().AllForThisElection.Where(v => v.BallotGuid == ballot.BallotGuid)
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
          vote.InvalidReasonGuid = VoteHelperLocal.IneligibleToReceiveVotes(person.IneligibleReasonGuid,
            person.CanReceiveVotes);
        }
        vote.InvalidReasonGuid = invalidReasonGuid;
        Db.Vote.Add(vote);
        Db.SaveChanges();

        new VoteCacher().UpdateItemAndSaveCache(vote);

        var ballotAnalyzer = new BallotAnalyzer();
        var rawBallot = CurrentRawBallot();
        var ballotStatusInfo = ballotAnalyzer.UpdateBallotStatus(rawBallot, VoteInfosFor(rawBallot), true);

        var sum = BallotCount(ballot.LocationGuid, isSingleName);

        new BallotCacher().UpdateItemAndSaveCache(rawBallot);

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
      var vote = new VoteCacher().AllForThisElection.SingleOrDefault(v => v.C_RowId == vid);
      if (vote == null)
      {
        return new { Message = "Not found" }.AsJsonResult();
      }

      new VoteCacher().RemoveItemAndSaveCache(vote);

      Db.Vote.Attach(vote);
      Db.Vote.Remove(vote);
      Db.SaveChanges();

      UpdateVotePositions(vote.BallotGuid);

      var ballotAnalyzer = new BallotAnalyzer();
      var ballot = CurrentRawBallot();
      var ballotStatusInfo = ballotAnalyzer.UpdateBallotStatus(ballot, VoteInfosFor(ballot), false);
      var isSingleName = UserSession.CurrentElection.IsSingleNameElection;
      var location = new LocationCacher().AllForThisElection.Single(l => l.LocationGuid == ballot.LocationGuid);

      var sum = BallotCount(location.LocationGuid, isSingleName);

      new BallotCacher().UpdateItemAndSaveCache(ballot);

      return new
      {
        Deleted = true,
        Votes = CurrentVotesForJs(),
        BallotStatus = ballotStatusInfo.Status.Value,
        BallotStatusText = ballotStatusInfo.Status.DisplayText,
        ballotStatusInfo.SpoiledCount,
        LocationBallotsEntered = sum
      }.AsJsonResult();
    }



    public static int BallotCount(Guid locationGuid, string computerCode, bool isSingleName, List<Ballot> ballots = null, List<Vote> votes = null)
    {
      int sum;
      ballots = ballots ?? new BallotCacher().AllForThisElection;

      if (isSingleName)
      {
        var allBallotGuids = ballots.Where(b => b.LocationGuid == locationGuid && b.ComputerCode == computerCode)
          .Select(b => b.BallotGuid).ToList();

        votes = votes ?? new VoteCacher().AllForThisElection;
        sum = votes.Where(v => allBallotGuids.Contains(v.BallotGuid))
          .Sum(vi => vi.SingleNameElectionCount).AsInt();
      }
      else
      {
        sum = ballots.Count(b => b.LocationGuid == locationGuid && b.ComputerCode == computerCode);
      }
      return sum;
    }

    public static int BallotCount(Guid locationGuid, bool isSingleName, List<Ballot> ballots = null )
    {
      int sum;
      ballots = ballots ?? new BallotCacher().AllForThisElection;

      if (isSingleName)
      {
        var allBallotGuids = ballots.Where(b => b.LocationGuid == locationGuid)
          .Select(b => b.BallotGuid).ToList();

        sum = new VoteCacher().AllForThisElection.Where(v => allBallotGuids.Contains(v.BallotGuid))
          .Sum(vi => vi.SingleNameElectionCount).AsInt();
      }
      else
      {
        sum = ballots.Count(b => b.LocationGuid == locationGuid);
      }
      return sum;
    }

    public string InvalidReasonsByIdJsonString()
    {
      return IneligibleReasonEnum.Items
        .Select(r => new
        {
          Id = r.IndexNum,
          r.Group,
          Desc = r.Description
        })
        .SerializedAsJsonString();

      //return Db.Reasons
      //  //.Where(r => r.ReasonGroup != ReasonGroupIneligible)
      //  .OrderByDescending(r => r.ReasonGroup) // put Ineligible at the bottom
      //  .ThenBy(r => r.SortOrder)
      //  .Select(r => new
      //                 {
      //                   Id = r.C_RowId,
      //                   Group = r.ReasonGroup + (r.ReasonGroup == ReasonGroupIneligible ? " (and not in list)" : ""),
      //                   Desc = r.ReasonDescription
      //                 })
      //  .SerializedAsJsonString();
    }

    public string InvalidReasonsByGuidJsonString()
    {
      return IneligibleReasonEnum.Items
        .Select(r => new
        {
          Guid = r.Value,
          r.Group,
          Desc = r.Description
        })
        .SerializedAsJsonString();
    }

    public object CurrentBallotsInfoList()
    {
      var ballots = new BallotCacher().AllForThisElection
        .Where(b => b.LocationGuid == UserSession.CurrentLocationGuid)
        .OrderBy(b => b.ComputerCode)
        .ThenBy(b => b.BallotNumAtComputer)
        .ToList();

      return BallotsInfoList(ballots);
    }

    /// <Summary>Current Ballot... could be null</Summary>
    public Ballot GetCurrentBallot(bool refresh = false)
    {
      var createIfNeeded = UserSession.CurrentElection.IsSingleNameElection;
      var currentBallotId = SessionKey.CurrentBallotId.FromSession(0);

      var ballotCacher = new BallotCacher();

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

          var voteInfos = VoteInfosFor(ballot);
          
          new BallotAnalyzer().UpdateBallotStatus(ballot, voteInfos, true);
          ballotCacher.UpdateItemAndSaveCache(ballot);
        }
      }

      return ballot;
    }

    #endregion

    public abstract object BallotInfoForJs(Ballot b);

    /// <Summary>Get the current Ballot. Only use when there is a ballot.</Summary>
    public Ballot CurrentRawBallot()
    {
      var ballotId = SessionKey.CurrentBallotId.FromSession(0);
      return new BallotCacher().AllForThisElection.Single(b => b.C_RowId == ballotId);
      //      return Db.Ballot.Single(b => b.C_RowId == ballotId);
    }

    public List<Vote> CurrentVotes()
    {
      var ballot = GetCurrentBallot();

      if (ballot == null)
      {
        return new List<Vote>();
      }
      return new VoteCacher().AllForThisElection.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();
    }

    public List<VoteInfo> VoteInfosFor(Ballot ballot)
    {
      if (ballot == null)
      {
        return new List<VoteInfo>();
      }

      return VoteInfosForBallot(ballot);
    }

    public static List<VoteInfo> VoteInfosForBallot(Ballot ballot)
    {
      return new VoteCacher().AllForThisElection
                 .Where(v => v.BallotGuid == ballot.BallotGuid)
                 .JoinMatchingOrNull(new PersonCacher().AllForThisElection, v => v.PersonGuid, p => p.PersonGuid, (v, p) => new { v, p })
                 .Select(g => new VoteInfo(g.v, UserSession.CurrentElection, ballot, UserSession.CurrentLocation, g.p))
                 .ToList();
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

    private void UpdateVotePositions(Guid ballotGuid)
    {
      var votes = new VoteCacher().AllForThisElection
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

      var ballot = new Ballot
      {
        BallotGuid = Guid.NewGuid(),
        LocationGuid = currentLocationGuid,
        ComputerCode = computerCode,
        BallotNumAtComputer = NextBallotNumAtComputer(),
        StatusCode = BallotStatusEnum.Empty,
        TellerAtKeyboard = UserSession.GetCurrentTeller(1),
        TellerAssisting = UserSession.GetCurrentTeller(2)
      };
      Db.Ballot.Add(ballot);
      Db.SaveChanges();

      new BallotCacher().UpdateItemAndSaveCache(ballot);

      SessionKey.CurrentBallotId.SetInSession(ballot.C_RowId);

      return ballot; //TODO: used to be view
    }

    public string NewBallotsJsonString(long lastRowVersion)
    {
      var ballots = new BallotCacher().AllForThisElection
        .Where(b => b.RowVersionInt > lastRowVersion)
        .ToList();

      return ballots.Any()
        ? BallotsInfoList(ballots).SerializedAsJsonString()
        : "";

      //todo...
    }

    private object BallotsInfoList(List<Ballot> ballots)
    {
      var maxRowVersion = ballots.Count == 0 ? 0 : ballots.Max(b => b.RowVersionInt);

      return new
      {
        Ballots = ballots.ToList().Select(BallotInfoForJs),
        Last = maxRowVersion
      };
    }
  }
}