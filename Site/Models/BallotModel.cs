using System.Web.Mvc;
using System.Web.Providers.Entities;
using TallyJ.Code;
using System.Linq;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class BallotModel : DataConnectedModel
  {
    public static class BallotStatusCode
    {
      public const string Ok = "Ok";
    }
    public static class VoteStatusCode
    {
      public const string Ok = "Ok";
    }

    public JsonResult SaveVoteSingle(int personId, int voteId, int count)
    {
      if (voteId != 0)
      {
        var currentVote = Db.Votes.SingleOrDefault(v => v.C_RowId == voteId);
        if (currentVote != null)
        {
          currentVote.SingleNameElectionCount = count;
          return new { Updated = true }.AsJsonResult();
        }
      }

      var person =
        Db.People.SingleOrDefault(p => p.C_RowId == personId && p.ElectionGuid == UserSession.CurrentElectionGuid);
      if (person != null)
      {
        var ballot = GetCurrentVoteSingleBallot();

        var nextVoteNum = 1 + Db.Votes.Where(v => v.BallotGuid == ballot.BallotGuid)
                                .OrderByDescending(v => v.PositionOnBallot)
                                .Take(1)
                                .Select(b => b.PositionOnBallot)
                                .SingleOrDefault();

        var vote = new Vote
                     {
                       PersonGuid = person.PersonGuid,
                       PersonRowVersion = person.C_RowVersion,
                       BallotGuid = ballot.BallotGuid,
                       PositionOnBallot = nextVoteNum,
                       StatusCode = VoteStatusCode.Ok,
                       SingleNameElectionCount = count
                     };
        Db.SaveChanges();

        return new { Updated = true, VoteId = vote.C_RowId }.AsJsonResult();

      }

      return new {Updated = false}.AsJsonResult();
    }

    private Ballot GetCurrentVoteSingleBallot()
    {
      var computerCode = SessionKey.ComputerCode.FromSession("");
      var currentLocationGuid = UserSession.CurrentLocationGuid;

      var ballot =
        Db.Ballots.SingleOrDefault(
          b => b.BallotNumAtComputer == 0 && b.LocationGuid == currentLocationGuid
               && b.ComputerCode == computerCode);

      if (ballot == null)
      {
        var nextBallotNum = 1 + Db.Ballots.Where(b => b.LocationGuid == currentLocationGuid
                                                && b.ComputerCode == computerCode)
                                                .OrderByDescending(b=>b.BallotNumAtComputer)
                                                .Take(1)
                                                .Select(b=>b.BallotNumAtComputer)
                                                .SingleOrDefault();
        ballot = new Ballot
                   {
                     LocationGuid = currentLocationGuid,
                     ComputerCode =  computerCode,
                     BallotNumAtComputer = nextBallotNum,
                     StatusCode = BallotStatusCode.Ok,
                     TellerAtKeyboard = UserSession.CurrentTellerAtKeyboard,
                     TellerAssisting = UserSession.CurrentTellerAssisting
                   };
        Db.SaveChanges();
      }
      return ballot;
    }
  }
}