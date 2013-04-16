using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Models;

namespace TallyJ.CoreModels
{
    public class ElectionAnalyzerNormal : ElectionAnalyzerCore, IElectionAnalyzer
    {

        public ElectionAnalyzerNormal()
        {
        }

        public ElectionAnalyzerNormal(IAnalyzerFakes fakes, Election election,
                                      List<vVoteInfo> voteinfos, List<Ballot> ballots,
                                      List<Person> people)
            : base(fakes, election, people, ballots, voteinfos)
        {
            //_locationInfos = locationInfos;
        }

        public ElectionAnalyzerNormal(Election election)
            : base(election)
        {
        }

        //private List<vLocationInfo> _locationInfos;
        ///// <Summary>Current location records</Summary>
        //public List<vLocationInfo> LocationInfos
        //{
        //  get
        //  {
        //    return _locationInfos ?? (_locationInfos = Db.vLocationInfoes
        //                                                 .Where(r => r.ElectionGuid == CurrentElection.ElectionGuid)
        //                                                 .ToList());
        //  }
        //}

        public override ResultSummary AnalyzeEverything()
        {
            PrepareResultSummaryCalc();

            ResultSummaryCalc.BallotsNeedingReview = Ballots.Count(BallotAnalyzer.BallotNeedsReview);

            ResultSummaryCalc.NumBallotsEntered = Ballots.Count;
            ResultSummaryCalc.TotalVotes = ResultSummaryCalc.NumBallotsEntered * TargetElection.NumberToElect;

            var invalidBallotGuids = Ballots.Where(b => b.StatusCode != "Ok").Select(b => b.BallotGuid).ToList();

            ResultSummaryCalc.SpoiledBallots = invalidBallotGuids.Count();
            ResultSummaryCalc.SpoiledVotes =
              VoteInfos.Count(vi => !invalidBallotGuids.Contains(vi.BallotGuid) && VoteAnalyzer.IsNotValid(vi));

            // collect only valid votes
            foreach (var ballot in Ballots.Where(bi => bi.StatusCode == BallotStatusEnum.Ok))
            {
                var ballotGuid = ballot.BallotGuid;
                foreach (var vVoteInfo in VoteInfos.Where(vi => vi.BallotGuid == ballotGuid && VoteAnalyzer.VoteIsValid(vi)))
                {
                    var voteInfo = vVoteInfo;

                    // get existing result record for this person, if available
                    var result =
                      Results.SingleOrDefault(r => r.C_RowId == voteInfo.ResultId || r.PersonGuid == voteInfo.PersonGuid);
                    if (result == null)
                    {
                        result = new Result
                                   {
                                       ElectionGuid = TargetElection.ElectionGuid,
                                       PersonGuid = voteInfo.PersonGuid.AsGuid()
                                   };
                        ResetValues(result);
                        Results.Add(result);
                        AddResult(result);
                    }

                    var voteCount = result.VoteCount.AsInt() + 1;
                    result.VoteCount = voteCount;
                }
            }

            DoAnalysisForTies();
            FinalizeSummaries();


            return ResultSummaryFinal;
        }
    }
}