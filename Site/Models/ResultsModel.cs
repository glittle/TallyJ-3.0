using System;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;

namespace TallyJ.Models
{
  public class ResultsModel : DataConnectedModel
  {
    private readonly IElectionAnalyzer analyzer;

    public ResultsModel()
    {
      analyzer = UserSession.CurrentElection.IsSingleNameElection.AsBool()
                   ? new ElectionAnalyzerSingleName() as IElectionAnalyzer
                   : new ElectionAnalyzerNormal();
    }

    public JsonResult CurrentResults
    {
      get
      {
        var resultSummaryAuto = analyzer.ResultSummaryAuto;

        // don't show any details if review is needed
        if (resultSummaryAuto.BallotsNeedingReview != 0)
        {
          var needReview = analyzer.VoteInfos.Where(ElectionAnalyzerCore.NeedReview)
            .Join(Db.Locations, vi => vi.LocationId, l => l.C_RowId, (vi, location) => new { vi, location })
            .Select(x => new
                            {
                              x.vi.LocationId,
                              x.vi.BallotId,
                              Ballot = x.location.Name + " - " + x.vi.C_BallotCode,
                            })
                            .Distinct()
                            .OrderBy(x => x.Ballot);

          return new
                   {
                     NeedReview = needReview,
                     NumBallots = resultSummaryAuto.BallotsReceived,
                     resultSummaryAuto.TotalVotes,
                     TotalInvalidVotes = resultSummaryAuto.SpoiledVotes,
                     TotalInvalidBallots = resultSummaryAuto.SpoiledBallots,
                   }.AsJsonResult();
        }

        var vResultInfos =
          Db.vResultInfoes.Where(ri => ri.ElectionGuid == UserSession.CurrentElectionGuid).OrderBy(ri => ri.Rank);

        //var tallyStatus = UserSession.CurrentElection.TallyStatus;

        return new
                 {
                   Votes = vResultInfos,
                   NumBallots = resultSummaryAuto.BallotsReceived,
                   resultSummaryAuto.TotalVotes,
                   TotalInvalidVotes = resultSummaryAuto.SpoiledVotes,
                   TotalInvalidBallots = resultSummaryAuto.SpoiledBallots,
                 }.AsJsonResult();
      }
    }

    public JsonResult FinalResults
    {
      get
      {
        var resultSummaryAuto = analyzer.ResultSummaryAuto;

        if (resultSummaryAuto.BallotsNeedingReview != 0)
        {
          // don't show any details if review is needed
          return new
                   {
                   }.AsJsonResult();
        }

        var currentElection = UserSession.CurrentElection;
        var numToShow = currentElection.ShowFullReport.AsBool() ? 99999 : currentElection.NumberToElect.AsInt();
        var numForChart = 10;

        var reportVotes =
          Db.vResultInfoes.Where(ri => ri.ElectionGuid == UserSession.CurrentElectionGuid).OrderBy(ri => ri.Rank).Take(numToShow);

        var chartVotes =
          Db.vResultInfoes.Where(ri => ri.ElectionGuid == UserSession.CurrentElectionGuid).OrderBy(ri => ri.Rank)
          .Select(ri => new
                          {
                            ri.Rank,
                            ri.VoteCount
                          })
          .Take(numForChart);

        var tallyStatus = currentElection.TallyStatus;
        return new
                 {
                   ReportVotes = reportVotes,
                   ChartVotes = chartVotes,
                   NumBallots = resultSummaryAuto.BallotsReceived,
                   resultSummaryAuto.TotalVotes,
                   TotalInvalidVotes = resultSummaryAuto.SpoiledVotes,
                   TotalInvalidBallots = resultSummaryAuto.SpoiledBallots,
                   resultSummaryAuto.NumEligibleToVote,
                   resultSummaryAuto.NumVoters,
                   Participation = resultSummaryAuto.NumEligibleToVote.AsInt() == 0 ? 0 : Math.Round((resultSummaryAuto.NumVoters.AsInt() * 100D) / resultSummaryAuto.NumEligibleToVote.AsInt(), 0),
                   Status = tallyStatus,
                   StatusText = ElectionTallyStatusEnum.TextFor(tallyStatus)
                 }.AsJsonResult();
      }
    }

    public void GenerateResults()
    {
      analyzer.GenerateResults();
    }
  }
}