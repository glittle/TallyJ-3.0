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

    /// <Summary>Current Ballot... could be null</Summary>
    public vBallotInfo GetCurrentBallotInfo(bool createIfNeeded = false)
    {
      var currentBallotId = SessionKey.CurrentBallotId.FromSession(0);
      var ballot = Db.vBallotInfoes.SingleOrDefault(b => b.C_RowId == currentBallotId);

      if (createIfNeeded)
      {
        return CreateBallot();
      }

      return ballot;
    }

    public JsonResult SwitchToBallotJson(int ballotId)
    {
      SetAsCurrentBallot(ballotId);

      var ballotInfo = GetCurrentBallotInfo();
      var location = Db.Locations.Single(l => l.LocationGuid == ballotInfo.LocationGuid);

      SessionKey.CurrentLocation.SetInSession(location);

      return new
               {
                 BallotInfo = new
                                {
                                  Ballot = BallotForJson(ballotInfo),
                                  Votes = CurrentVotesForJson(),
                                  NumNeeded = UserSession.CurrentElection.NumberToElect
                                },
                 Location = ContextItems.LocationModel.LocationInfoForJson(location)
               }.AsJsonResult();
    }

    public bool SortVotes(List<int> ids)
    {
      var ballotGuid = CurrentBallot().BallotGuid;
      var votes = Db.Votes
        .Where(v => v.BallotGuid == ballotGuid)
        .ToList();

      var position = 1;
      foreach (var vote in ids.Select(id => votes.SingleOrDefault(v => v.C_RowId == id)).Where(vote => vote != null))
      {
        vote.PositionOnBallot = position;
        position++;
      }
      Db.SaveChanges();
      return true;
    }

    public JsonResult StartNewBallotJson()
    {
      var ballotInfo = CreateBallot();

      return new
               {
                 BallotInfo = new
                                {
                                  Ballot = BallotForJson(ballotInfo),
                                  Votes = CurrentVotesForJson(),
                                  NumNeeded = UserSession.CurrentElection.NumberToElect
                                },
                 Ballots = CurrentBallotsInfoList()
               }.AsJsonResult();
    }

    /// <Summary>Delete a ballot, but only if already empty</Summary>
    public JsonResult DeleteBallotJson()
    {
      var ballot = CurrentBallot();
      var ballotGuid = ballot.BallotGuid;

      var hasVotes = Db.Votes.Any(v => v.BallotGuid == ballotGuid);

      if (hasVotes)
      {
        return new
                 {
                   Deleted = false,
                   Message = "Can only delete a ballot when it has no votes."
                 }.AsJsonResult();
      }

      Db.Ballots.Remove(ballot);
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
      var ballot = CurrentBallot();

      ballot.StatusCode = needsReview ? BallotStatusEnum.Review : BallotStatusEnum.Ok;

      var ballotAnalyzer = new BallotAnalyzer();
      var ballotStatusInfo = ballotAnalyzer.UpdateBallotStatus(ballot, CurrentVoteInfoes());

      Db.SaveChanges();

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
      var ballotInfo =
        Db.vBallotInfoes.SingleOrDefault(b => b.C_RowId == ballotId && b.ElectionGuid == UserSession.CurrentElectionGuid);

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
      //  Db.Computers.Attach(computer);

      //  computer.ComputerCode = wantedComputerCode;
      //  computer.LastContact = DateTime.Now;
      //  Db.SaveChanges();

      //  SessionKey.CurrentComputer.SetInSession(computer);
      //}

      SessionKey.CurrentBallotId.SetInSession(ballotInfo.C_RowId);
    }

    public abstract int NextBallotNumAtComputer();

    public string CurrentBallotJsonString()
    {
      var ballotInfo = GetCurrentBallotInfo();
      if (ballotInfo == null)
      {
        return "null";
      }

      return new
               {
                 Ballot = BallotForJson(ballotInfo),
                 Votes = CurrentVotesForJson(),
                 NumNeeded = UserSession.CurrentElection.NumberToElect
               }.SerializedAsJsonString();
    }

    public IEnumerable<object> CurrentVotesForJson()
    {
      return CurrentVoteInfoes()
        .OrderBy(v => v.PositionOnBallot)
        .Select(v => new
                       {
                         vid = v.VoteId,
                         count = v.SingleNameElectionCount,
                         pid = v.PersonId,
                         pos = v.PositionOnBallot,
                         name = v.PersonFullName,
                         changed = !Equals(v.PersonCombinedInfo, v.PersonCombinedInfoInVote),
                         invalid = v.VoteIneligibleReasonGuid,
                         ineligible = VoteHelperLocal.IneligibleToReceiveVotes(v.PersonIneligibleReasonGuid, v.CanReceiveVotes)
                       });
    }

    public JsonResult SaveVote(int personId, int voteId, int count, Guid invalidReason)
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;

      if (voteId != 0)
      {
        // update existing record

        // find info about the existing Vote
        var voteInfo =
          Db.vVoteInfoes.SingleOrDefault(vi => vi.VoteId == voteId && vi.ElectionGuid == currentElectionGuid);

        if (voteInfo == null)
        {
          // problem... client has a vote number, but we didn't find...
          return new { Updated = false, Error = "Invalid vote id" }.AsJsonResult();
        }

        var vote = Db.Votes.Single(v => v.C_RowId == voteInfo.VoteId);

        vote.SingleNameElectionCount = count;
        vote.PersonCombinedInfo = voteInfo.PersonCombinedInfo;

        DetermineInvalidReasonGuid(invalidReason, vote);

        Db.SaveChanges();

        var ballotAnalyzer = new BallotAnalyzer();
        var ballotStatusInfo = ballotAnalyzer.UpdateBallotStatus(CurrentBallot(), CurrentVoteInfoes());

        return new
                 {
                   Updated = true,
                   BallotStatus = ballotStatusInfo.Status.Value,
                   BallotStatusText = ballotStatusInfo.Status.DisplayText,
                   ballotStatusInfo.SpoiledCount
                 }.AsJsonResult();
      }

      var shouldCreateBallotIfNeeded = UserSession.CurrentElection.IsSingleNameElection;
      var ballot = GetCurrentBallotInfo(shouldCreateBallotIfNeeded);
      if (ballot == null)
      {
        return new {Updated = false, Error = "Invalid ballot"}.AsJsonResult();
      }

      // don't have an active Ballot!
      // make a new Vote record

      var invalidReasonGuid = DetermineInvalidReasonGuid(invalidReason);

      var person = Db.People.SingleOrDefault(p => p.C_RowId == personId && p.ElectionGuid == currentElectionGuid);

      var ok = person != null || invalidReasonGuid != Guid.Empty;

      if (ok)
      {
        var nextVoteNum = 1 + Db.Votes.Where(v => v.BallotGuid == ballot.BallotGuid)
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
          vote.InvalidReasonGuid = VoteHelperLocal.IneligibleToReceiveVotes(person.IneligibleReasonGuid, person.CanReceiveVotes);
        }
        if (invalidReasonGuid != Guid.Empty)
        {
          vote.InvalidReasonGuid = invalidReasonGuid.AsNullableGuid();
        }
        Db.Votes.Add(vote);
        Db.SaveChanges();

        var ballotAnalyzer = new BallotAnalyzer();
        var ballotStatusInfo = ballotAnalyzer.UpdateBallotStatus(CurrentBallot(), CurrentVoteInfoes());

        return new
                 {
                   Updated = true,
                   VoteId = vote.C_RowId,
                   pos = vote.PositionOnBallot,
                   BallotStatus = ballotStatusInfo.Status.Value,
                   BallotStatusText =  ballotStatusInfo.Status.DisplayText,
                   ballotStatusInfo.SpoiledCount
                 }.AsJsonResult();
      }

      // don't recognize person id
      return new {Updated = false, Error = "Invalid person"}.AsJsonResult();
    }

    public JsonResult DeleteVote(int vid)
    {
      var voteInfo =
        Db.vVoteInfoes.SingleOrDefault(vi => vi.ElectionGuid == UserSession.CurrentElectionGuid && vi.VoteId == vid);
      if (voteInfo == null)
      {
        return new {Message = "Not found"}.AsJsonResult();
      }

      var vote = Db.Votes.Single(v => v.C_RowId == vid);
      Db.Votes.Remove(vote);
      Db.SaveChanges();

      UpdateVotePositions(voteInfo.BallotGuid);

      var ballotAnalyzer = new BallotAnalyzer();
      var ballotStatusInfo = ballotAnalyzer.UpdateBallotStatus(CurrentBallot(), CurrentVoteInfoes());

      return new
               {
                 Deleted = true,
                 Votes = CurrentVotesForJson(),
                 BallotStatus = ballotStatusInfo.Status.Value,
                 BallotStatusText = ballotStatusInfo.Status.DisplayText,
                 ballotStatusInfo.SpoiledCount
               }.AsJsonResult();
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
      var ballots = Db.vBallotInfoes
        .Where(
          b => b.ElectionGuid == UserSession.CurrentElectionGuid && b.LocationGuid == UserSession.CurrentLocationGuid)
        .OrderBy(b => b.ComputerCode)
        .ThenBy(b => b.BallotNumAtComputer);

      return BallotsInfoList(ballots);
    }

    #endregion

    /// <Summary>Get the current Ballot. Only use when there is a ballot.</Summary>
    public Ballot CurrentBallot()
    {
      var ballotId = SessionKey.CurrentBallotId.FromSession(0);
      return Db.Ballots.Single(b => b.C_RowId == ballotId);
    }

    public List<Vote> CurrentVotes()
    {
      var ballot = GetCurrentBallotInfo();

      if (ballot == null)
      {
        return new List<Vote>();
      }
      return Db.Votes.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();
    }

    public List<vVoteInfo> CurrentVoteInfoes()
    {
      var ballot = GetCurrentBallotInfo();

      if (ballot == null)
      {
        return new List<vVoteInfo>();
      }
      return Db.vVoteInfoes.Where(v => v.BallotGuid == ballot.BallotGuid).ToList();
    }

    /// <Summary>Convert int to Guid for InvalidReason. If vote is given, assign if different</Summary>
    private Guid DetermineInvalidReasonGuid(Guid invalidReasonGuid, Vote vote = null)
    {
      if (vote != null && vote.InvalidReasonGuid != invalidReasonGuid)
      {
        vote.InvalidReasonGuid = invalidReasonGuid.AsNullableGuid();
      }

      return invalidReasonGuid;
    }

    private void UpdateVotePositions(Guid ballotGuid)
    {
      var votes = Db.Votes
        .Where(v => v.BallotGuid == ballotGuid)
        .OrderBy(v => v.PositionOnBallot)
        .ToList();

      var position = 1;
      votes.ForEach(v => v.PositionOnBallot = position++);

      Db.SaveChanges();
    }

    public vBallotInfo CreateBallot()
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
      Db.Ballots.Add(ballot);
      Db.SaveChanges();

      SessionKey.CurrentBallotId.SetInSession(ballot.C_RowId);

      return Db.vBallotInfoes.Single(bi => bi.C_RowId == ballot.C_RowId);
    }

    public string NewBallotsJsonString(int lastRowVersion)
    {
      var ballots = Db.vBallotInfoes
        .Where(b => b.ElectionGuid == UserSession.CurrentElectionGuid
                    && b.RowVersionInt > lastRowVersion);

      return ballots.Any()
               ? BallotsInfoList(ballots).SerializedAsJsonString()
               : "";

      //todo...
    }

    private object BallotsInfoList(IQueryable<vBallotInfo> ballots)
    {
      var maxRowVersion = ballots.Max(b => b.RowVersionInt);

      return new
               {
                 Ballots = ballots.ToList().Select(BallotForJson),
                 Last = maxRowVersion
               };
    }

    public abstract object BallotForJson(vBallotInfo b);
  }
}