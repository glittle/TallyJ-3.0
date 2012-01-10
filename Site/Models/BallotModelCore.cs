using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public abstract class BallotModelCore : DataConnectedModel, IBallotModel
  {
    public const string ReasonGroupIneligible = "Ineligible";

    #region IBallotModel Members

    /// <Summary>Current Ballot... could be null</Summary>
    public vBallotInfo GetCurrentBallotInfo(bool createIfNeeded)
    {
      var currentBallotId = SessionKey.CurrentBallotId.FromSession(0);
      var ballot = Db.vBallotInfoes.SingleOrDefault(b => b.C_RowId == currentBallotId);

      if (ballot == null && createIfNeeded)
      {
        return CreateBallot();
      }

      return ballot;
    }

    public JsonResult SwitchToBallotJson(int ballotId)
    {
      SetAsCurrentBallot(ballotId);

      var ballotInfo = GetCurrentBallotInfo(false);
      var location = Db.Locations.Single(l => l.LocationGuid == ballotInfo.LocationGuid);

      SessionKey.CurrentLocation.SetInSession(location);

      return new
               {
                 BallotInfo = new
                                {
                                  Ballot = BallotForJson(ballotInfo),
                                  Votes = CurrentVotes(),
                                  NumNeeded = UserSession.CurrentElection.NumberToElect
                                },
                 Location = new LocationModel().LocationInfoForJson(location)
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
      var ballotInfo = GetCurrentBallotInfo(true);
      if (ballotInfo == null)
      {
        return "null";
      }

      return new
               {
                 Ballot = BallotForJson(ballotInfo),
                 Votes = CurrentVotes(),
                 NumNeeded = UserSession.CurrentElection.NumberToElect
               }.SerializedAsJsonString();
    }

    public IEnumerable<object> CurrentVotes()
    {
      var ballot = GetCurrentBallotInfo(false);

      if (ballot == null)
      {
        return new List<object>();
      }

      var vVoteInfos = Db.vVoteInfoes.Where(v => v.BallotGuid == ballot.BallotGuid);

      var currentVotes = vVoteInfos
        .OrderBy(v => v.PositionOnBallot)
        .ToList() //avoid LINQ issues
        .Select(v => new
                       {
                         vid = v.VoteId,
                         count = v.SingleNameElectionCount,
                         pid = v.PersonId,
                         pos = v.PositionOnBallot,
                         name = v.PersonFullName,
                         changed = !Equals(v.PersonCombinedInfo, v.PersonCombinedInfoInVote),
                         invalid = v.VoteInvalidReasonId,
                         ineligible = v.PersonIneligibleReasonId
                       });
      return currentVotes;
    }

    public JsonResult SaveVote(int personId, int voteId, int count, int invalidReason)
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;

      if (voteId != 0)
      {
        // update existing record

        var voteInfo =
          Db.vVoteInfoes.SingleOrDefault(vi => vi.VoteId == voteId && vi.ElectionGuid == currentElectionGuid);

        if (voteInfo == null)
        {
          return new { Updated = false, Error = "Invalid vote id" }.AsJsonResult();
        }

        // problem... client has a vote number, but we didn't find...
        // TODO : deal with this?
        var vote = Db.Votes.Single(v => v.C_RowId == voteInfo.VoteId);

        vote.SingleNameElectionCount = count;
        vote.PersonCombinedInfo = voteInfo.PersonCombinedInfo;

        DetermineInvalidReasonGuid(invalidReason, vote);

        Db.SaveChanges();

        return new
                 {
                   Updated = true,
                   //Location = new LocationModel().CurrentBallotLocationInfo()
                 }.AsJsonResult();
      }

      var ballot = GetCurrentBallotInfo(true);
      if (ballot == null)
      {
        return new { Updated = false, Error = "Invalid ballot" }.AsJsonResult();
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
                       StatusCode = BallotHelper.VoteStatusCode.Ok,
                       SingleNameElectionCount = count
                     };
        if (person != null)
        {
          vote.PersonGuid = person.PersonGuid;
          vote.PersonCombinedInfo = person.CombinedInfo;
        }
        if (invalidReasonGuid != Guid.Empty)
        {
          vote.InvalidReasonGuid = invalidReasonGuid.AsNullableGuid();
        }
        Db.Votes.Add(vote);
        Db.SaveChanges();

        return new
                 {
                   Updated = true,
                   VoteId = vote.C_RowId,
                   pos = vote.PositionOnBallot,
                   // Location = new LocationModel().CurrentBallotLocationInfo()
                 }.AsJsonResult();
      }

      // don't recognize person id
      return new { Updated = false, Error = "Invalid person" }.AsJsonResult();
    }

    /// <Summary>Convert int to Guid for InvalidReason. If vote is given, assign if different</Summary>
    private Guid DetermineInvalidReasonGuid(int invalidReason, Vote vote = null)
    {
      if (invalidReason == 0)
      {
        return Guid.Empty;
      }

      var invalidReasonGuid = Db.Reasons.Where(r => r.C_RowId == invalidReason).Select(r => r.ReasonGuid).SingleOrDefault();
      if (invalidReasonGuid == Guid.Empty)
      {
        // didn't get a valid reason - use the last Ineligible reason... should be "Other"
        invalidReasonGuid =
          Db.Reasons.Where(r => r.ReasonGroup == "Ineligible").OrderByDescending(r => r.SortOrder).Select(
            r => r.ReasonGuid).First();
      }

      if (vote != null && vote.InvalidReasonGuid != invalidReasonGuid)
      {
        vote.InvalidReasonGuid = invalidReasonGuid.AsNullableGuid();
      }

      return invalidReasonGuid;
    }

    public JsonResult DeleteVote(int vid)
    {
      var voteInfo =
        Db.vVoteInfoes.SingleOrDefault(vi => vi.ElectionGuid == UserSession.CurrentElectionGuid && vi.VoteId == vid);
      if (voteInfo == null)
      {
        return new { Message = "Not found" }.AsJsonResult();
      }

      var vote = Db.Votes.Single(v => v.C_RowId == vid);
      Db.Votes.Remove(vote);
      Db.SaveChanges();

      UpdateVotePositions(voteInfo.BallotGuid);

      return new
               {
                 Deleted = true,
                 Votes = CurrentVotes(),
                 NumNeeded = UserSession.CurrentElection.NumberToElect
               }.AsJsonResult();
    }

    public string InvalidReasonsJsonString()
    {
      return Db.Reasons
        //.Where(r => r.ReasonGroup != ReasonGroupIneligible)
        .OrderByDescending(r => r.ReasonGroup) // put Ineligible at the bottom
        .ThenBy(r => r.SortOrder)
        .Select(r => new
                       {
                         Id = r.C_RowId,
                         Group = r.ReasonGroup + (r.ReasonGroup==ReasonGroupIneligible ? " (and not in list)" : ""),
                         Desc = r.ReasonDescription
                       })
        .SerializedAsJsonString();
    }

    public string CurrentBallotsJsonString()
    {
      var ballots = Db.vBallotInfoes
        .Where(b => b.ElectionGuid == UserSession.CurrentElectionGuid)
        .OrderBy(b => b.ComputerCode)
        .ThenBy(b => b.BallotNumAtComputer);

      return BallotsList(ballots);
    }

    #endregion

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
                       StatusCode = BallotHelper.BallotStatusCode.InEdit,
                       TellerAtKeyboard = UserSession.CurrentTellerAtKeyboard,
                       TellerAssisting = UserSession.CurrentTellerAssisting
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
               ? BallotsList(ballots)
               : "";

      //todo...
    }

    private string BallotsList(IQueryable<vBallotInfo> ballots)
    {
      var maxRowVersion = ballots.Max(b => b.RowVersionInt);

      return new
               {
                 Ballots = ballots.ToList().Select(BallotForJson),
                 Last = maxRowVersion
               }.SerializedAsJsonString();
    }

    protected abstract object BallotForJson(vBallotInfo b);
  }
}