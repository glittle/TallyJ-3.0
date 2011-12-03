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
    /// <Summary>Current Ballot... could be null</Summary>
    public Ballot GetCurrentBallot()
    {
      var currentBallotId = SessionKey.CurrentBallotId.FromSession(0);
      var ballot = Db.Ballots.SingleOrDefault(b => b.C_RowId == currentBallotId);
      return ballot;
    }

    /// <Summary>Switch current ballot</Summary>
    public void SetAsCurrentBallot(int ballotId)
    {
      if (ballotId == SessionKey.CurrentBallotId.FromSession(0))
      {
        return;
      }

      // learn about the one wanted
      var ballotInfo = Db.vBallots.SingleOrDefault(b => b.C_RowId == ballotId && b.ElectionGuid == UserSession.CurrentElectionGuid);

      if (ballotInfo == null)
      {
        // invalid request!?
        return;
      }

      var wantedComputerCode = ballotInfo.ComputerCode;
      if (wantedComputerCode != UserSession.CurrentComputerCode)
      {
        // get our record, update it, and save it back
        var computer = UserSession.CurrentComputer;
        Db.Computers.Attach(computer);

        computer.ComputerCode = wantedComputerCode;
        Db.SaveChanges();

        SessionKey.CurrentComputer.SetInSession(computer);
      }

      SessionKey.CurrentBallotId.SetInSession(ballotInfo.C_RowId);
    }

    public Ballot CreateBallot()
    {
      var currentLocationGuid = UserSession.CurrentLocationGuid;
      var computerCode = UserSession.CurrentComputerCode;

      var ballot = new Ballot
                     {
                       BallotGuid = Guid.NewGuid(),
                       LocationGuid = currentLocationGuid,
                       ComputerCode = computerCode,
                       BallotNumAtComputer = NextBallotNumAtComputer(),
                       // for single  ballots, always use #0
                       StatusCode = BallotHelper.BallotStatusCode.Ok,
                       TellerAtKeyboard = UserSession.CurrentTellerAtKeyboard,
                       TellerAssisting = UserSession.CurrentTellerAssisting
                     };
      Db.Ballots.Add(ballot);
      Db.SaveChanges();

      SessionKey.CurrentBallotId.SetInSession(ballot.C_RowId);

      return ballot;
    }

    public abstract int NextBallotNumAtComputer();

    public string CurrentVotesJson()
    {
      return CurrentVotes().SerializedAsJsonString();
    }

    public IEnumerable<object> CurrentVotes()
    {
      var ballot = GetCurrentBallot();

      if (ballot == null)
      {
        return new List<object>();
      }

      var currentVotes = Db.vVoteInfoes
        .Where(v => v.BallotGuid == ballot.BallotGuid)
        .OrderBy(v => v.PositionOnBallot)
        .Select(v => new
                       {
                         vid = v.VoteId,
                         count = v.SingleNameElectionCount,
                         pid = v.PersonId,
                         name = v.PersonFullName,
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
        var existingVoteInfo = Db.vVoteInfoes
          .Join(Db.Votes, info => info.VoteId, vote => vote.C_RowId, (info, vote) => new { info, vote })
          .SingleOrDefault(iv => iv.vote.C_RowId == voteId && iv.info.ElectionGuid == currentElectionGuid);
        if (existingVoteInfo != null)
        {
          existingVoteInfo.vote.SingleNameElectionCount = count;

          if (invalidReason != 0)
          {
            var invalidReasonGuid = Db.Reasons.Where(r => r.C_RowId == invalidReason).Select(r => r.ReasonGuid).SingleOrDefault();
            if (invalidReasonGuid != Guid.Empty && invalidReasonGuid != existingVoteInfo.vote.InvalidReasonGuid)
            {
              existingVoteInfo.vote.InvalidReasonGuid = invalidReasonGuid;
            }
          }

          Db.SaveChanges();

          return new { Updated = true }.AsJsonResult();
        }

        // problem... client has a vote number, but we didn't find...
        // TODO : deal with this?
      }
      else
      {
        var ballot = GetCurrentBallot();
        if (ballot != null)
        {
          // make a new Vote record
          var ok = false;
          var invalidReasonGuid = Guid.Empty;
          Person person = null;

          if (invalidReason != 0)
          {
            invalidReasonGuid = Db.Reasons.Where(r => r.C_RowId == invalidReason).Select(r => r.ReasonGuid).SingleOrDefault();
            ok = invalidReasonGuid != Guid.Empty;
          }
          else
          {
            person = Db.People.SingleOrDefault(p => p.C_RowId == personId && p.ElectionGuid == currentElectionGuid);
            ok = person != null;
          }
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
              vote.PersonRowVersion = person.C_RowVersion;
            }
            if (invalidReasonGuid != Guid.Empty)
            {
              vote.InvalidReasonGuid = invalidReasonGuid.AsNullableGuid();
            }
            Db.Votes.Add(vote);
            Db.SaveChanges();

            return new { Updated = true, VoteId = vote.C_RowId }.AsJsonResult();
          }
        }
        else
        {
          // don't recognize person id
        }
      }

      return new { Updated = false }.AsJsonResult();
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

      return new
               {
                 Deleted = true,
                 AllVotes = CurrentVotes()
               }.AsJsonResult();
    }

    public string InvalidReasonsJson()
    {
      return Db.Reasons
        .OrderByDescending(r => r.ReasonGroup) // put Inelligible at the bottom
        .ThenBy(r => r.SortOrder)
        .Select(r => new
                       {
                         Id = r.C_RowId,
                         Group = r.ReasonGroup,
                         Desc = r.ReasonDescription
                       })
        .SerializedAsJsonString();
    }
  }
}