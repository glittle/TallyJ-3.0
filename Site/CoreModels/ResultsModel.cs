using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.Models;

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
                _analyzer.GetOrCreateResultSummaries();

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
                    Db.Results.Where(ri => ri.ElectionGuid == CurrentElection.ElectionGuid)
                      .Join(Db.People, r => r.PersonGuid, p => p.PersonGuid, (r, p) => new { r, PersonName = p.C_FullNameFL })
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
                            reportVotes.Select(g => new { g.PersonName, g.r.VoteCount, g.r.TieBreakCount, g.r.Section }),
                        NumBallots = resultSummaryFinal.NumBallotsWithManual,
                        resultSummaryFinal.TotalVotes,
                        TotalInvalidVotes = resultSummaryFinal.SpoiledVotes,
                        TotalInvalidBallots = resultSummaryFinal.SpoiledBallots,
                        resultSummaryFinal.NumEligibleToVote,
                        resultSummaryFinal.NumVoters,
                        Participation =
                            resultSummaryFinal.NumEligibleToVote.AsInt() == 0
                                ? 0
                                : Math.Round(
                                    (resultSummaryFinal.NumBallotsWithManual.AsInt() * 100D) /
                                    resultSummaryFinal.NumEligibleToVote.AsInt(), 0),
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
            var resultSummaries = _analyzer.ResultSummaries;
            var resultSummaryFinal = resultSummaries.First(rs => rs.ResultType == ResultType.Final);

            // don't show any details if review is needed
            if (resultSummaryFinal.BallotsNeedingReview != 0)
            {
                var locations = Db.Locations.Where(l => l.ElectionGuid == UserSession.CurrentElectionGuid).ToList();

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

            var vResultInfos =
                Db.Results
                  .Where(ri => ri.ElectionGuid == CurrentElection.ElectionGuid)
                  .Join(Db.People, r => r.PersonGuid, p => p.PersonGuid, (r, p) => new { r, PersonName = p.C_FullNameFL })
                  .OrderBy(g => g.r.Rank)
                  .Select(g => new
                      {
                          // TODO 2012-01-21 Glen Little: Could return fewer columns for non-tied results
                          rid = g.r.C_RowId,
                          g.r.CloseToNext,
                          g.r.CloseToPrev,
                          g.r.ForceShowInOther,
                          g.r.IsTied,
                          g.r.IsTieResolved,
                          g.PersonName,
                          g.r.Rank,
                          //ri.RankInExtra,
                          g.r.Section,
                          g.r.TieBreakCount,
                          g.r.TieBreakGroup,
                          g.r.TieBreakRequired,
                          g.r.VoteCount
                      });

            var ties = Db.ResultTies.Where(rt => rt.ElectionGuid == CurrentElection.ElectionGuid)
                         .OrderBy(rt => rt.TieBreakGroup)
                         .Select(rt => new
                             {
                                 rt.TieBreakGroup,
                                 rt.NumInTie,
                                 rt.NumToElect,
                                 rt.TieBreakRequired,
                                 rt.IsResolved
                             });

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
            var summary =
                Db.ResultSummaries.SingleOrDefault(
                    rs =>
                    rs.ElectionGuid == UserSession.CurrentElectionGuid && rs.ResultType == ResultType.Final);
            var readyForReports = summary != null && summary.UseOnReports.AsBoolean();

            object data;
            switch (code)
            {
                case "Ballots":
                    var ballots = Db.vBallotInfoes.Where(b => b.ElectionGuid == CurrentElection.ElectionGuid).ToList();
                    var votes = Db.vVoteInfoes.Where(b => b.ElectionGuid == CurrentElection.ElectionGuid).ToList();
                    data = ballots
                        .OrderBy(b => b.ComputerCode)
                        .ThenBy(b => b.BallotNumAtComputer)
                        .Select(b => new
                            {
                                b.C_BallotCode,
                                b.StatusCode,
                                Spoiled = b.StatusCode != BallotStatusEnum.Ok,
                                Rows2 = votes
                                         .Where(v => v.BallotGuid == b.BallotGuid).OrderBy(v => v.PositionOnBallot)
                                         .Select(v => new
                                             {
                                                 v.PersonFullNameFL,
                                                 Spoiled = v.VoteStatusCode != VoteHelper.VoteStatusCode.Ok,
                                                 VoteInvalidReasonDesc =
                                                          IneligibleReasonEnum.DescriptionFor(
                                                              v.VoteIneligibleReasonGuid.AsGuid()).
                                                                               SurroundContentWith("[", "]")
                                             })
                            });

                    break;

                case "AllReceivingVotes":
                case "AllReceivingVotesByVote":
                    var rows = Db.Results.Where(r => r.ElectionGuid == CurrentElection.ElectionGuid)
                           .Join(Db.People, r => r.PersonGuid, p => p.PersonGuid, (r, p) => new { r, p });
                    if (code == "AllReceivingVotes")
                    {
                        rows = rows.OrderBy(g => g.p.C_FullNameFL);
                    }
                    else
                    {
                        rows = rows.OrderByDescending(g => g.r.VoteCount)
                                   .ThenBy(g => g.p.C_FullNameFL);
                    }
                    data = rows.Select(g =>
                                       new
                                           {
                                               PersonName = g.p.C_FullNameFL,
                                               g.r.VoteCount
                                           }
                        );
                    break;

                case "SimpleResults":

                    if (summary == null)
                    {
                        return new
                            {
                                Status = "Results not available",
                                ElectionStatus = CurrentElection.TallyStatus,
                                ElectionStatusText = ElectionTallyStatusEnum.TextFor(CurrentElection.TallyStatus)
                            }.AsJsonResult();
                    }

                    var currentElection = UserSession.CurrentElection;
                    var result = Db.Results
                                   .Where(r => r.ElectionGuid == UserSession.CurrentElectionGuid)
                                   .OrderBy(r => r.Rank)
                                   .Take(currentElection.NumberToElect.AsInt() + currentElection.NumberExtra.AsInt())
                                   .ToList()
                                   .Join(Db.People.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid),
                                         r => r.PersonGuid,
                                         p => p.PersonGuid,
                                         (result1, person) => new { person, result1 })
                                   .ToList();

                    return new
                        {
                            People = result.Select(rp =>
                                                   new
                                                       {
                                                           Name = rp.person.C_FullName,
                                                           rp.person.BahaiId,
                                                           Rank = rp.result1.Section == ResultHelper.Section.Extra
                                                                      ? "Next " + rp.result1.RankInExtra
                                                                      : rp.result1.Rank.ToString(),
                                                           VoteCountPlus = rp.result1.VoteCount.GetValueOrDefault() +
                                                                           (rp.result1.TieBreakCount.GetValueOrDefault() ==
                                                                            0
                                                                                ? ""
                                                                                : " / {0}".FilledWith(
                                                                                    rp.result1.TieBreakCount)),
                                                       }
                                ),
                            Info = new
                                {
                                    Name = UserSession.CurrentElectionName,
                                    Final = summary,
                                    Pct =
                                        summary.NumEligibleToVote.GetValueOrDefault() == 0
                                            ? 0
                                            : Math.Round(100.0 * summary.NumBallotsEntered.GetValueOrDefault() /
                                                         summary.NumEligibleToVote.GetValueOrDefault())
                                },
                            Status = "ok",
                            ElectionStatus = CurrentElection.TallyStatus,
                            Ready = readyForReports,
                            ElectionStatusText = ElectionTallyStatusEnum.TextFor(CurrentElection.TallyStatus)
                        }.AsJsonResult();

                default:
                    return new { Status = "Unknown report" }.AsJsonResult();
            }

            return new
                {
                    Rows = data,
                    Status = "ok",
                    Ready = readyForReports,
                    ElectionStatus = CurrentElection.TallyStatus,
                    ElectionStatusText = ElectionTallyStatusEnum.TextFor(CurrentElection.TallyStatus)
                }.AsJsonResult();
        }

        public JsonResult SaveTieCounts(List<string> counts)
        {
            // input like:   2_3,5_3,235_0
            var countItems = counts.Select(delegate(string s)
                {
                    var parts = s.SplitWithString("_", StringSplitOptions.None);
                    return new
                        {
                            ResultId = parts[0].AsInt(),
                            Value = parts[1].AsInt()
                        };
                }).ToList();
            var resultsIds = countItems.Select(ci => ci.ResultId).ToArray();

            var results =
                Db.Results.Where(r => resultsIds.Contains(r.C_RowId) && r.ElectionGuid == CurrentElection.ElectionGuid)
                  .ToList();

            foreach (var result in results)
            {
                result.TieBreakCount = countItems.Single(ci => ci.ResultId == result.C_RowId).Value;
            }

            Db.SaveChanges();

            return new
                {
                    Status = "Saved"
                }.AsJsonResult();
        }

        public JsonResult SaveManualResults(ResultSummary manualResultsFromBrowser)
        {
            ResultSummary resultSummary = null;
            if (manualResultsFromBrowser.C_RowId != 0)
            {
                resultSummary = Db.ResultSummaries.FirstOrDefault(rs =>
                                                                  rs.C_RowId == manualResultsFromBrowser.C_RowId
                                                                  && rs.ElectionGuid == UserSession.CurrentElectionGuid
                                                                  && rs.ResultType == ResultType.Manual);
            }
            if (resultSummary == null)
            {
                resultSummary = new ResultSummary
                    {
                        ElectionGuid = UserSession.CurrentElectionGuid,
                        ResultType = ResultType.Manual
                    };
                Db.ResultSummaries.Add(resultSummary);
            }

            var editableFields = new
                {
                    resultSummary.BallotsNeedingReview,
                    resultSummary.NumBallotsEntered,
                    resultSummary.EnvelopesCalledIn,
                    resultSummary.EnvelopesDroppedOff,
                    resultSummary.EnvelopesInPerson,
                    resultSummary.EnvelopesMailedIn,
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

            _analyzer.GetOrCreateResultSummaries();
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