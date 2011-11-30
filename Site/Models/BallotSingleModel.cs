using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class BallotSingleModel : DataConnectedModel
  {
    private Ballot GetCurrentBallot()
    {
      var computerCode = SessionKey.ComputerCode.FromSession("");
      var currentLocationGuid = UserSession.CurrentLocationGuid;

      var ballot =
        Db.Ballots.SingleOrDefault(
          b => b.BallotNumAtComputer == 0 && b.LocationGuid == currentLocationGuid
               && b.ComputerCode == computerCode);

      if (ballot == null)
      {
        //var nextBallotNum = 1 + Db.Ballots.Where(b => b.LocationGuid == currentLocationGuid
        //                                              && b.ComputerCode == computerCode)
        //                          .OrderByDescending(b => b.BallotNumAtComputer)
        //                          .Take(1)
        //                          .Select(b => b.BallotNumAtComputer)
        //                          .SingleOrDefault();
        ballot = new Ballot
                   {
                     BallotGuid = Guid.NewGuid(),
                     LocationGuid = currentLocationGuid,
                     ComputerCode = computerCode,
                     BallotNumAtComputer = 0,
                     StatusCode = BallotHelper.BallotStatusCode.Ok,
                     TellerAtKeyboard = UserSession.CurrentTellerAtKeyboard,
                     TellerAssisting = UserSession.CurrentTellerAssisting
                   };
        Db.Ballots.Add(ballot);
        Db.SaveChanges();
      }
      return ballot;
    }

    public string CurrentVotesJson()
    {
      return CurrentVotes().SerializedAsJson();
    }

    public IEnumerable<object> CurrentVotes()
    {
      var ballot = GetCurrentBallot();

      return Db.vVoteInfoes.Where(v => v.BallotGuid == ballot.BallotGuid)
        .OrderBy(v => v.PositionOnBallot)
        .Select(v => new
                       {
                         vid = v.VoteRowId,
                         count = v.SingleNameElectionCount,
                         pid = v.PersonRowId,
                         name = v.PersonFullName
                       });
    }

    public JsonResult SaveVote(int personId, int voteId, int count)
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;

      if (voteId != 0)
      {
        var existingVoteInfo = Db.vVoteInfoes
          .Join(Db.Votes, info => info.VoteRowId, vote => vote.C_RowId, (info, vote) => new {info, vote})
          .SingleOrDefault(iv => iv.vote.C_RowId == voteId && iv.info.ElectionGuid == currentElectionGuid);
        if (existingVoteInfo != null)
        {
          existingVoteInfo.vote.SingleNameElectionCount = count;
          Db.SaveChanges();

          return new { Updated = true }.AsJsonResult();
        }

        // problem... client has a vote number, but we didn't find...
        // TODO : deal with this?
      }
      else
      {
        // make a new Vote record
        var person = Db.People.SingleOrDefault(p => p.C_RowId == personId && p.ElectionGuid == currentElectionGuid);
        if (person != null)
        {
          var ballot = GetCurrentBallot();

          var nextVoteNum = 1 + Db.Votes.Where(v => v.BallotGuid == ballot.BallotGuid)
                                  .OrderByDescending(v => v.PositionOnBallot)
                                  .Take(1)
                                  .Select(b => b.PositionOnBallot)
                                  .SingleOrDefault();

          var vote = new Vote
                       {
                         BallotGuid = ballot.BallotGuid,
                         PositionOnBallot = nextVoteNum,
                         PersonGuid = person.PersonGuid,
                         PersonRowVersion = person.C_RowVersion,
                         StatusCode = BallotHelper.VoteStatusCode.Ok,
                         SingleNameElectionCount = count
                       };
          Db.Votes.Add(vote);
          Db.SaveChanges();

          return new {Updated = true, VoteId = vote.C_RowId}.AsJsonResult();
        }
        else
        {
          // don't recognize person id
        }
      }

      return new {Updated = false}.AsJsonResult();
    }

    public JsonResult DeleteVote(int vid)
    {
      var voteInfo = Db.vVoteInfoes.SingleOrDefault(vi => vi.ElectionGuid == UserSession.CurrentElectionGuid && vi.VoteRowId == vid);
      if (voteInfo == null)
      {
        return new {Message = "Not found"}.AsJsonResult();
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
  }
}