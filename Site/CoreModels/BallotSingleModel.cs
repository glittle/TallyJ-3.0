using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class BallotSingleModel : BallotModelCore
  {
    public override int NextBallotNumAtComputer()
    {
      // for single name ballots, always use 1
      return 1;
    }

    public override object CurrentBallotsInfoList(bool refresh = false)
    {
      if (refresh)
      {
        new ElectionAnalyzerNormal().RefreshBallotStatuses(); // identical for single name elections
        Db.SaveChanges();
      }

      var votes = new VoteCacher(Db).AllForThisElection;
      //      var ballotGuidsWithVotes = votes.Select(v => v.BallotGuid).Distinct().ToList();

      var ballots = new BallotCacher(Db).AllForThisElection;

      var ballotsForLocation = ballots
        .Where(b => b.LocationGuid == UserSession.CurrentLocationGuid)
        //        .Where(b => ballotGuidsWithVotes.Contains(b.BallotGuid))
        .OrderBy(b => b.ComputerCode)
        .ThenBy(b => b.BallotNumAtComputer)
        .ToList();

      var ballotGuids = ballotsForLocation.Select(b => b.BallotGuid).ToList();

      var totalCount = votes
        .Where(v => ballotGuids.Contains(v.BallotGuid))
        .Sum(vi => vi.SingleNameElectionCount).AsInt();

      return new
      {
        Ballots = ballotsForLocation.ToList().Select(ballot => BallotInfoForJs(ballot, votes)),
        Total = totalCount
      };
    }

    public override object BallotInfoForJs(Ballot b, List<Vote> allVotes)
    {
      var votes = allVotes
        .Where(v => v.BallotGuid == b.BallotGuid)
        .ToList();

      var ballotCounts = votes.Sum(v => v.SingleNameElectionCount);

      var hasOnlineRaw = votes.Any(v => v.OnlineVoteRaw.HasContent());
      var hasOnlineRawToFinish = votes.Any(v => v.OnlineVoteRaw.HasContent() && v.PersonGuid == null && v.InvalidReasonGuid == null);

      return new
      {
        Id = b.C_RowId,
        Code = b.C_BallotCode,
        b.ComputerCode,
        Count = ballotCounts,
        people = votes.Count,
        hasOnlineRaw,
        hasOnlineRawToFinish
      };
    }

    public override JsonResult StartNewBallotJson()
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }

      // for single name, only one ballot per computer
      var ballots = new BallotCacher(Db).AllForThisElection;
      if (ballots.Any(b => b.ComputerCode == UserSession.CurrentComputerCode))
      {
        return new { Message = "Only one 'ballot' per computer in single-name elections." }.AsJsonResult();
      }

      var locationModel = new LocationModel();
      if (locationModel.HasLocationsWithoutOnline && UserSession.CurrentLocation == null)
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
        BallotInfo = BallotInfoForJs(ballotInfo, allVotes),
        Ballots = CurrentBallotsInfoList()
      }.AsJsonResult();
    }
  }
}