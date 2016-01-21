using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ResultsModel : DataConnectedModel
  {
    private readonly IElectionAnalyzer _analyzer;
    private Election _election;

    public ResultsModel(Election election = null)
    {
      _election = election;

      _analyzer = CurrentElection.IsSingleNameElection
        ? new ElectionAnalyzerSingleName(election) as IElectionAnalyzer
        : new ElectionAnalyzerNormal(election);
    }

    private Election CurrentElection
    {
      get { return _election ?? (_election = UserSession.CurrentElection); }
    }

    public JsonResult FinalResultsJson
    {
      get
      {
        _analyzer.PrepareResultSummaries();

        var resultSummaryFinal = _analyzer.ResultSummaryFinal;

        if (!resultSummaryFinal.UseOnReports.AsBoolean())
        {
          // don't show any details if review is needed
          return new
          {
            StatusText = "Analysis not complete",
            Status = "Errors"
          }.AsJsonResult();
        }

        //currentElection.ShowFullReport.AsBool() ? 99999 : 
        var numToShow = CurrentElection.NumberToElect.AsInt();
        var numExtra = CurrentElection.NumberExtra.AsInt();
        //                var numForChart = (numToShow + numExtra) * 2;

        var reportVotes =
          new ResultCacher().AllForThisElection
            .Join(new PersonCacher().AllForThisElection, r => r.PersonGuid, p => p.PersonGuid,
              (r, p) => new { r, PersonName = p.FullNameFL })
            .OrderBy(g => g.r.Rank)
            .Take(numToShow + numExtra);

        //var chartVotes =
        //  Db.vResultInfoes.Where(ri => ri.ElectionGuid == CurrentElection.ElectionGuid).OrderBy(ri => ri.Rank)
        //    .Select(ri => new
        //                    {
        //                      ri.Rank,
        //                      ri.VoteCount
        //                    })
        //    .Take(numForChart);

        var tallyStatus = CurrentElection.TallyStatus;

        if (tallyStatus != ElectionTallyStatusEnum.Report)
        {
          return new
          {
            Status = tallyStatus,
            StatusText = ElectionTallyStatusEnum.TextFor(tallyStatus)
          }.AsJsonResult();
        }

        return new
        {
          ReportVotes =
            reportVotes.Select(g => new
            {
              g.PersonName,
              g.r.VoteCount,
              TieBreakCount = g.r.IsTied.AsBoolean() ? g.r.TieBreakCount : null,
              g.r.Section
            }),
          NumBallots = resultSummaryFinal.NumBallotsWithManual,
          resultSummaryFinal.TotalVotes,
          TotalInvalidVotes = resultSummaryFinal.SpoiledVotes,
          TotalInvalidBallots = resultSummaryFinal.SpoiledBallots,
          resultSummaryFinal.NumEligibleToVote,
          EnvelopesMailedIn = resultSummaryFinal.MailedInBallots,
          EnvelopesInPerson = resultSummaryFinal.InPersonBallots,
          EnvelopesDroppedOff = resultSummaryFinal.DroppedOffBallots,
          EnvelopesCalledIn = resultSummaryFinal.CalledInBallots,
          resultSummaryFinal.NumVoters,
          Participation = resultSummaryFinal.PercentParticipation,
          Status = tallyStatus,
          StatusText = ElectionTallyStatusEnum.TextFor(tallyStatus)
        }.AsJsonResult();
      }
    }

    public JsonResult GetCurrentResultsJson()
    {
      return GetCurrentResults().AsJsonResult();
    }

    public object GetCurrentResults()
    {
      var localResultSummaryCacher = _analyzer.LocalResultSummaryCacher;
      localResultSummaryCacher.DropThisCache();

      var resultSummaries = localResultSummaryCacher.AllForThisElection;

      try
      {
        var resultSummaryFinal = resultSummaries.SingleOrDefault(rs => rs.ResultType == ResultType.Final);


        // don't show any details if review is needed
        if (resultSummaryFinal.BallotsNeedingReview != 0)
        {
          var locations = new LocationCacher().AllForThisElection;

          var needReview = _analyzer.VoteInfos.Where(VoteAnalyzer.VoteNeedReview)
            .Join(locations, vi => vi.LocationId, l => l.C_RowId,
              (vi, location) => new { vi, location })
            .Select(x => new
            {
              x.vi.LocationId,
              x.vi.BallotId,
              Status =
                x.vi.BallotStatusCode == "Review"
                  ? BallotStatusEnum.Review.DisplayText
                  : "Verification Needed",
              Ballot =
                string.Format("{0} ({1})", x.vi.C_BallotCode, x.location.Name)
            })
            .Distinct()
            .OrderBy(x => x.Ballot);

          var needReview2 = _analyzer.Ballots.Where(b => b.StatusCode == BallotStatusEnum.Review)
            .Join(locations, b => b.LocationGuid, l => l.LocationGuid,
              (b, location) => new { b, location })
            .Select(x => new
            {
              LocationId = x.location.C_RowId,
              BallotId = x.b.C_RowId,
              Status =
                x.b.StatusCode == "Review"
                  ? BallotStatusEnum.Review.DisplayText
                  : "Verification Needed",
              Ballot =
                string.Format("{0} ({1})", x.b.C_BallotCode, x.location.Name)
            });

          return new
          {
            NeedReview = needReview.Concat(needReview2).Distinct(),
            ResultsManual =
              (resultSummaries.FirstOrDefault(rs => rs.ResultType == ResultType.Manual) ??
               new ResultSummary()).GetPropertiesExcept(null, new[] { "ElectionGuid" }),
            ResultsCalc =
              resultSummaries.First(rs => rs.ResultType == ResultType.Calculated)
                .GetPropertiesExcept(null, new[] { "ElectionGuid" }),
            ResultsFinal =
              resultSummaries.First(rs => rs.ResultType == ResultType.Final)
                .GetPropertiesExcept(null, new[] { "ElectionGuid" }),
          };
        }

        // show vote totals

        var persons = new PersonCacher().AllForThisElection;

        var vResultInfos =
          // TODO 2012-01-21 Glen Little: Could return fewer columns for non-tied results
          new ResultCacher().AllForThisElection
            .OrderBy(r => r.Rank)
            .ToList()
            .Select(r => new
            {
              rid = r.C_RowId,
              r.CloseToNext,
              r.CloseToPrev,
              r.ForceShowInOther,
              r.IsTied,
              r.IsTieResolved,
              PersonName = PersonNameFor(persons, r),
              r.Rank,
              //ri.RankInExtra,
              r.Section,
              r.TieBreakCount,
              r.TieBreakGroup,
              r.TieBreakRequired,
              r.VoteCount
            }).ToList();

        var ties = new ResultTieCacher().AllForThisElection
          .OrderBy(rt => rt.TieBreakGroup)
          .Select(rt => new
          {
            rt.TieBreakGroup,
            rt.NumInTie,
            rt.NumToElect,
            rt.TieBreakRequired,
            rt.IsResolved
          }).ToList();

        //var spoiledVotesSummary = Db.vVoteInfoes.where

        return new
        {
          Votes = vResultInfos,
          Ties = ties,
          NumToElect = _election.NumberToElect,
          NumExtra = _election.NumberExtra,
          ShowCalledIn = _election.UseCallInButton,
          ResultsManual =
            (resultSummaries.FirstOrDefault(rs => rs.ResultType == ResultType.Manual) ?? new ResultSummary())
              .GetPropertiesExcept(null, new[] { "ElectionGuid" }),
          ResultsCalc =
            resultSummaries.First(rs => rs.ResultType == ResultType.Calculated)
              .GetPropertiesExcept(null, new[] { "ElectionGuid" }),
          ResultsFinal =
            resultSummaries.First(rs => rs.ResultType == ResultType.Final)
              .GetPropertiesExcept(null, new[] { "ElectionGuid" }),
        };
      }
      catch (Exception ex)
      {
        return new
        {
          Interrupted = true,
          Msg = ex.GetAllMsgs("; ") + "\n" + ex.StackTrace
        };
      }

    }

    private string PersonNameFor(IEnumerable<Person> persons, Result result)
    {
      var personGuid = result.PersonGuid;
      var person = persons.FirstOrDefault(p => p.PersonGuid == personGuid);
      return person == null ? "?" : person.FullNameFL;
    }

    public void GenerateResults()
    {
      _analyzer.AnalyzeEverything();
    }

    public object GetCurrentResultsIfAvailable()
    {
      return _analyzer.IsResultAvailable ? GetCurrentResults() : null;
    }

    public JsonResult GetReportData(string code)
    {
      var localResultSummaryCacher = _analyzer.LocalResultSummaryCacher;
      localResultSummaryCacher.DropThisCache();
      var summary = localResultSummaryCacher.AllForThisElection.SingleOrDefault(rs => rs.ResultType == ResultType.Final);
      var readyForReports = summary != null && summary.UseOnReports.AsBoolean();

      var status = "ok";
      var electionStatus = CurrentElection.TallyStatus;

      var html = "";
      switch (code)
      {
        case "SimpleResults":
          var currentElection = CurrentElection;
          if (summary == null)
          {
            status = "Results not available. Please view 'Analyze' page first.";
            electionStatus = currentElection.TallyStatus;
          }
          else
          {
            html = MvcViewRenderer.RenderRazorViewToString("~/Reports/Main.cshtml", summary);
          }
          break;

        default:
          html = MvcViewRenderer.RenderRazorViewToString("~/Reports/{0}.cshtml".FilledWith(code));
          if (html.HasNoContent())
          {
            return new { Status = "Unknown report" }.AsJsonResult();
          }
          break;
      }

      return new
      {
        Html = html,
        Status = status,
        Ready = readyForReports,
        ElectionStatus = electionStatus,
        ElectionStatusText = ElectionTallyStatusEnum.TextFor(electionStatus)
      }.AsJsonResult();
    }

    public JsonResult SaveTieCounts(List<string> counts)
    {
      if (counts == null || counts.Count == 0)
      {
        return new
        {
          Msg = "Nothing to Save"
        }.AsJsonResult();
      }
      // input like:   2_3,5_3,235_0
      var countItems = counts.Select(delegate (string s)
      {
        var parts = s.SplitWithString("_", StringSplitOptions.None);
        return new
        {
          ResultId = parts[0].AsInt(),
          Value = parts[1].AsInt()
        };
      }).ToList();

      var resultsIds = countItems.Select(ci => ci.ResultId).ToArray();

      var resultCacher = new ResultCacher();
      var results = resultCacher.AllForThisElection.Where(r => resultsIds.Contains(r.C_RowId)).ToList();

      foreach (var result in results)
      {
        Db.Result.Attach(result);

        result.TieBreakCount = countItems.Single(ci => ci.ResultId == result.C_RowId).Value;

        resultCacher.UpdateItemAndSaveCache(result);
      }

      Db.SaveChanges();

      return new
      {
        Saved = true
      }.AsJsonResult();
    }

    public JsonResult SaveManualResults(ResultSummary manualResultsFromBrowser)
    {
      ResultSummary resultSummary = null;
      var resultSummaryCacher = new ResultSummaryCacher();

      if (manualResultsFromBrowser.C_RowId != 0)
      {
        resultSummary = resultSummaryCacher.AllForThisElection.FirstOrDefault(rs =>
          rs.C_RowId == manualResultsFromBrowser.C_RowId
          && rs.ResultType == ResultType.Manual);
      }
      if (resultSummary == null)
      {
        resultSummary = new ResultSummary
        {
          ElectionGuid = UserSession.CurrentElectionGuid,
          ResultType = ResultType.Manual
        };
        Db.ResultSummary.Add(resultSummary);
        Db.SaveChanges();

        resultSummaryCacher.UpdateItemAndSaveCache(resultSummary);
      }
      else
      {
        Db.ResultSummary.Attach(resultSummary);
      }

      var editableFields = new
      {
        resultSummary.BallotsNeedingReview,
        resultSummary.BallotsReceived,
        resultSummary.CalledInBallots,
        resultSummary.DroppedOffBallots,
        resultSummary.InPersonBallots,
        resultSummary.MailedInBallots,
        resultSummary.NumEligibleToVote,
        resultSummary.SpoiledManualBallots,
      }.GetAllPropertyInfos().Select(pi => pi.Name).ToArray();

      var changed = manualResultsFromBrowser.CopyPropertyValuesTo(resultSummary, editableFields);

      if (!changed)
      {
        return new
        {
          Message = "No changes"
        }.AsJsonResult();
      }

      Db.SaveChanges();


      resultSummaryCacher.UpdateItemAndSaveCache(resultSummary);

      _analyzer.PrepareResultSummaries();
      _analyzer.FinalizeSummaries();

      var resultSummaries = _analyzer.ResultSummaries;

      return new
      {
        Saved = true,
        ResultsManual =
          (resultSummaries.FirstOrDefault(rs => rs.ResultType == ResultType.Manual) ??
           new ResultSummary())
            .GetPropertiesExcept(null, new[] { "ElectionGuid" }),
        ResultsFinal =
          resultSummaries.First(rs => rs.ResultType == ResultType.Final)
            .GetPropertiesExcept(null, new[] { "ElectionGuid" }),
      }.AsJsonResult();
    }
  }
}