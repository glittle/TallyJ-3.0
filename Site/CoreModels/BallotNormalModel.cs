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
  public class BallotNormalModel : BallotModelCore
  {
    public override int NextBallotNumAtComputer()
    {
      var computerCode = UserSession.CurrentComputerCode;

      var nextBallotNum = 1 + new BallotCacher(Db).AllForThisElection.Where(b => b.ComputerCode == computerCode)
                                .OrderByDescending(b => b.BallotNumAtComputer)
                                .Take(1)
                                .Select(b => b.BallotNumAtComputer)
                                .SingleOrDefault();

      return nextBallotNum;
    }

    public override object CurrentBallotsInfoList(bool refresh = false)
    {
      if (refresh)
      {
        new ElectionAnalyzerNormal().RefreshBallotStatuses(); // identical for single name elections
        Db.SaveChanges();
      }

      var filter = UserSession.CurrentBallotFilter;
      if (UserSession.CurrentLocationName == LocationModel.OnlineLocationName
       || UserSession.CurrentLocationName == LocationModel.ImportedLocationName)
      {
        // ignore filter for online
        filter = null;
      }

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

    private object BallotsInfoList(List<Ballot> ballots, int totalCount)
    {
      return new
      {
        Ballots = ballots.ToList().Select(ballot => BallotInfoForJs(ballot, new VoteCacher(Db).AllForThisElection)),
        Total = totalCount
      };
    }

    public override object BallotInfoForJs(Ballot b, List<Vote> allVotes)
    {
      var spoiledCount = b.StatusCode != BallotStatusEnum.Ok ? 0 : (allVotes ?? new VoteCacher(Db).AllForThisElection)
        .Count(v => v.BallotGuid == b.BallotGuid && v.StatusCode != VoteStatusCode.Ok);
      return new
      {
        Id = b.C_RowId,
        Guid = b.ComputerCode == ComputerModel.ComputerCodeForImported ? (Guid?)b.BallotGuid : null,
        Code = b.C_BallotCode,
        b.StatusCode,
        StatusCodeText = BallotStatusEnum.TextFor(b.StatusCode),
        SpoiledCount = spoiledCount
      };
    }

    public override JsonResult StartNewBallotJson()
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
  }
}