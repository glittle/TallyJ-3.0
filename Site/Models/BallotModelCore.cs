using System;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public abstract class BallotModelCore : DataConnectedModel, IBallotModel
  {
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

      return new
      {
        BallotInfo = new
          {
            Ballot = BallotForJson(ballotInfo),
            Votes = CurrentVotes()
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
                 Votes = CurrentVotes()
               }.SerializedAsJsonString();
    }

    public IEnumerable<object> CurrentVotes()
    {
      var ballot = GetCurrentBallotInfo(false);

      if (ballot == null)
      {
        return new List<object>();
      }

      var currentVotes = Db.vVoteInfoes
        .Where(v => v.BallotGuid == ballot.BallotGuid)
        .OrderBy(v => v.PositionOnBallot)
        .ToList() //avoid LINQ issues
        .Select(v => new
                       {
                         vid = v.VoteId,
                         count = v.SingleNameElectionCount,
                         pid = v.PersonId,
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
        //var existingVoteInfo = Db.vVoteInfoes
        //  .Join(Db.Votes, info => info.VoteId, vote => vote.C_RowId, (info, vote) => new { info, vote })
        //  .SingleOrDefault(iv => iv.vote.C_RowId == voteId && iv.info.ElectionGuid == currentElectionGuid);

        var voteInfo = Db.vVoteInfoes.SingleOrDefault(vi => vi.VoteId == voteId && vi.ElectionGuid == currentElectionGuid);

        if (voteInfo != null)
        {
          var vote = Db.Votes.Single(v => v.C_RowId == voteInfo.VoteId);
          
          vote.SingleNameElectionCount = count;
          vote.PersonCombinedInfo = voteInfo.PersonCombinedInfo;

          if (invalidReason != 0)
          {
            var invalidReasonGuid =
              Db.Reasons.Where(r => r.C_RowId == invalidReason).Select(r => r.ReasonGuid).SingleOrDefault();
            if (invalidReasonGuid != Guid.Empty && invalidReasonGuid != vote.InvalidReasonGuid)
            {
              vote.InvalidReasonGuid = invalidReasonGuid;
            }
          }

          Db.SaveChanges();

          return new
                   {
                     Updated = true,
                     Location = new LocationModel().CurrentBallotLocationInfo()
                   }.AsJsonResult();
        }

        // problem... client has a vote number, but we didn't find...
        // TODO : deal with this?
      }
      else
      {
        var ballot = GetCurrentBallotInfo(true);
        if (ballot != null)
        {
          // make a new Vote record
          var ok = false;
          var invalidReasonGuid = Guid.Empty;
          Person person = null;

          if (invalidReason != 0)
          {
            invalidReasonGuid =
              Db.Reasons.Where(r => r.C_RowId == invalidReason).Select(r => r.ReasonGuid).SingleOrDefault();
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
              vote.PersonCombinedInfo = person.CombinedInfo;
            }
            if (invalidReasonGuid != Guid.Empty)
            {
              vote.InvalidReasonGuid = invalidReasonGuid.AsNullableGuid();
            }
            Db.Votes.Add(vote);
            Db.SaveChanges();

            return new { 
              Updated = true, 
              VoteId = vote.C_RowId,
              Location = new LocationModel().CurrentBallotLocationInfo()
            }.AsJsonResult();
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
                 //AllVotes = CurrentVotes(),
                 Location = new LocationModel().CurrentBallotLocationInfo()
               }.AsJsonResult();
    }

    public const string ReasonGroupIneligible = "Ineligible";

    public string InvalidReasonsJsonString()
    {

      return Db.Reasons
        //.Where(r => r.ReasonGroup != ReasonGroupIneligible)
        .OrderBy(r => r.ReasonGroup) // put Inelligible at the bottom
        .ThenBy(r => r.SortOrder)
        .Select(r => new
                       {
                         Id = r.C_RowId,
                         Group = r.ReasonGroup,
                         Desc = r.ReasonDescription
                       })
        .SerializedAsJsonString();
    }

    public string CurrentBallotsJsonString()
    {
      var ballots = Db.vBallotInfoes
        .Where(b => b.ElectionGuid == UserSession.CurrentElectionGuid);

      return BallotsList(ballots);
    }

    #endregion

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
                       StatusCode = BallotHelper.BallotStatusCode.Ok,
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