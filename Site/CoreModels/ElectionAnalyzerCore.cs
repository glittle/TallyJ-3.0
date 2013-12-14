using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
    public abstract class ElectionAnalyzerCore : DataConnectedModel
    {
        private const int ThresholdForCloseVote = 3;

        private readonly Func<Result, Result> _addResult;
        private readonly Func<ResultSummary, ResultSummary> _addResultSummary;
        private readonly Func<ResultTie, ResultTie> _addResultTie;
        private readonly Func<Result, Result> _deleteResult;
        private readonly Func<ResultTie, ResultTie> _deleteResultTie;
        private readonly Func<int> _saveChanges;
        private BallotAnalyzer _ballotAnalyzer;
        private List<Ballot> _ballots;
        private Election _election;
        private List<Person> _people;
        private List<ResultSummary> _resultSummaries;
        private List<ResultTie> _resultTies;
        private List<Result> _results;
        private List<VoteInfo> _voteinfos;
        private List<Vote> _votes;

        protected ElectionAnalyzerCore()
        {
        }

        protected ElectionAnalyzerCore(IAnalyzerFakes fakes, Election election, List<Person> people,
                                       List<Ballot> ballots,
                                       List<VoteInfo> voteinfos)
        {
            _election = election;
            _resultTies = new List<ResultTie>();
            _results = new List<Result>();
            _resultSummaries = new List<ResultSummary> {fakes.ResultSummaryManual};
            _people = people;
            _ballots = ballots;
            _voteinfos = voteinfos;
            _votes = voteinfos.Select(vi => new Vote {C_RowId = vi.VoteId}).ToList();
            _deleteResult = fakes.RemoveResult;
            _addResult = fakes.AddResult;
            _addResultSummary = fakes.AddResultSummary;
            _saveChanges = fakes.SaveChanges;
            _deleteResultTie = fakes.RemoveResultTie;
            _addResultTie = fakes.AddResultTie;
          IsFaked = true;
        }

      public bool IsFaked{ get; private set; }

      protected ElectionAnalyzerCore(Election election)
        {
            _election = election;
        }

        public ResultSummary ResultSummaryCalc { get; private set; }
        public ResultSummary ResultSummaryFinal { get; private set; }
        public ResultSummary ResultSummaryManual { get; private set; }

        protected BallotAnalyzer BallotAnalyzer
        {
            get { return _ballotAnalyzer ?? (_ballotAnalyzer = new BallotAnalyzer(TargetElection, SaveChanges)); }
        }

        /// <Summary>Remove this result from the datastore</Summary>
        protected Func<Result, Result> RemoveResult
        {
            get { return _deleteResult ?? Db.Result.Remove; }
        }

        /// <Summary>Remove this result from the datastore</Summary>
        protected Func<ResultTie, ResultTie> RemoveResultTie
        {
            get { return _deleteResultTie ?? Db.ResultTie.Remove; }
        }

        /// <Summary>Save all datastore changes</Summary>
        protected Func<int> SaveChanges
        {
            get { return _saveChanges ?? Db.SaveChanges; }
        }

        /// <Summary>Add this result to the datastore</Summary>
        protected Func<Result, Result> AddResult
        {
            get { return _addResult ?? Db.Result.Add; }
        }

        /// <Summary>Add this result to the datastore</Summary>
        protected Func<ResultSummary, ResultSummary> AddResultSummary
        {
            get
            {
                if (_addResultSummary != null) return _addResultSummary;
                return Db.ResultSummary.Add;
            }
        }

        /// <Summary>Add this resultTie to the datastore</Summary>
        protected Func<ResultTie, ResultTie> AddResultTie
        {
            get { return _addResultTie ?? Db.ResultTie.Add; }
        }

        /// <Summary>Current Results records</Summary>
        public List<Person> People
        {
            get
            {
                return _people ?? (_people = new PersonCacher().AllForThisElection.ToList());
            }
        }

        /// <Summary>Current Results records</Summary>
        public List<ResultTie> ResultTies
        {
            get
            {
                return _resultTies ?? (_resultTies = Db.ResultTie
                                                       .Where(p => p.ElectionGuid == TargetElection.ElectionGuid)
                                                       .ToList());
            }
        }

        internal Election TargetElection
        {
            get { return _election ?? (_election = UserSession.CurrentElection); }
        }

        /// <Summary>Votes are loaded, in case DB updates are required.</Summary>
        public List<Vote> Votes
        {
            get
            {
                if (_votes != null) return _votes;

                var voteIds = VoteInfos.Select(vi => vi.VoteId).ToList();

                return _votes = new VoteCacher().AllForThisElection.Where(v => voteIds.Contains(v.C_RowId)).ToList();
            }
        }

        #region IElectionAnalyzer Members

        public List<Ballot> Ballots
        {
            get { return _ballots ?? (_ballots = new BallotCacher().AllForThisElection.ToList()); }
        }

        /// <Summary>Current Results records</Summary>
        public List<Result> Results
        {
            get
            {
                return _results ?? (_results = Db.Result
                                                 .Where(r => r.ElectionGuid == TargetElection.ElectionGuid)
                                                 .ToList());
            }
        }

        public List<ResultSummary> ResultSummaries
        {
            get
            {
                if (_resultSummaries != null) return _resultSummaries;

                var list = new[] { ResultType.Manual, ResultType.Calculated, ResultType.Final };
                return _resultSummaries = Db.ResultSummary
                                            .Where(
                                                r => r.ElectionGuid == TargetElection.ElectionGuid
                                                     && list.Contains(r.ResultType))
                                            .ToList();
            }
        }

        /// <Summary>Current VoteInfo records. They are detached, so no updates can be done</Summary>
        public List<VoteInfo> VoteInfos
        {
            get
            {
                if (_voteinfos != null) return _voteinfos;

               return _voteinfos = new VoteCacher().AllForThisElection
                 .JoinMatchingOrNull(new PersonCacher().AllForThisElection, v => v.PersonGuid, p => p.PersonGuid, (v, p) => new { v, p })
                 .Select(g => new VoteInfo(g.v, new BallotCacher().AllForThisElection.Single(b=>b.BallotGuid==g.v.BallotGuid), UserSession.CurrentLocation, g.p))
                 .ToList();
//
//                else
//                    _voteinfos = Db.vVoteInfoes
//                                   .Where(vi => vi.ElectionGuid == TargetElection.ElectionGuid)
//                                   .OrderBy(vi => vi.BallotGuid)
//                                   .ToList();
//                _voteinfos.ForEach(Db.Detach);
//                return _voteinfos;
            }
        }

        /// <Summary>Check locally and in DB to see if the result is available at the moment</Summary>
        public bool IsResultAvailable
        {
            get
            {
                if (ResultSummaryFinal != null)
                {
                    return true;
                }

                ResultSummaryFinal =
                    Db.ResultSummary.FirstOrDefault(
                        rs => rs.ElectionGuid == TargetElection.ElectionGuid && rs.ResultType == ResultType.Final);

                return ResultSummaryFinal != null;
            }
        }

        /// <Summary>In the Core, do some common results generation</Summary>
        public abstract ResultSummary AnalyzeEverything();

        public void PrepareResultSummaryCalc()
        {
            // first refresh all votes and ballots
            if (VoteAnalyzer.UpdateAllStatuses(VoteInfos, Votes))
            {
                SaveChanges();
            }
            BallotAnalyzer.UpdateAllBallotStatuses(Ballots, VoteInfos);

            // clear any existing results
            Results.ForEach(ResetValues);

            GetOrCreateResultSummaries();
            FillCalcSummary();
        }

        /// <summary>
        ///     Load the Calc and Final summaries
        /// </summary>
        public void GetOrCreateResultSummaries()
        {
            if (ResultSummaryCalc != null && ResultSummaryFinal != null)
            {
                return;
            }

            // check each on on its own
            if (ResultSummaryCalc == null)
            {
                ResultSummaryCalc = ResultSummaries.FirstOrDefault(rs => rs.ResultType == ResultType.Calculated);
                if (ResultSummaryCalc == null)
                {
                    ResultSummaryCalc = new ResultSummary
                        {
                            ElectionGuid = TargetElection.ElectionGuid,
                            ResultType = ResultType.Calculated
                        };
                    AddResultSummary(ResultSummaryCalc);
                    ResultSummaries.Insert(0, ResultSummaryCalc);
                }
            }

            if (ResultSummaryFinal == null)
            {
                ResultSummaryFinal = ResultSummaries.FirstOrDefault(rs => rs.ResultType == ResultType.Final);
                if (ResultSummaryFinal == null)
                {
                    ResultSummaryFinal = new ResultSummary
                        {
                            ElectionGuid = TargetElection.ElectionGuid,
                            ResultType = ResultType.Final
                        };
                    AddResultSummary(ResultSummaryFinal);
                    ResultSummaries.Insert(0, ResultSummaryFinal);
                }
            }
            //try
            //{
            //    SaveChanges();
            //}
            //catch (DbEntityValidationException ex)
            //{
            //    var msgs = new List<string>();
            //    foreach (var msg in ex.EntityValidationErrors.Where(v => !v.IsValid).Select(validationResult =>
            //        {
            //            var err = validationResult.ValidationErrors.First();
            //            return "{0}: {1}".FilledWith(err.PropertyName, err.ErrorMessage);
            //        }).Where(msg => !msgs.Contains(msg)))
            //    {
            //        msgs.Add(msg);
            //    }
            //    throw new ApplicationException("Unable to save: " + msgs.JoinedAsString("; "));
            //}

        }

        ///// <Summary>Current Results records.  If not available in memory or DB, make an empty one</Summary>
        //public ResultSummary ResultSummaryFinal
        //{
        //    get
        //    {
        //        if (ResultSummaryFinal == null)
        //        {
        //            ResultSummaryFinal = Db.ResultSummary.FirstOrDefault(rs => rs.ElectionGuid == TargetElection.ElectionGuid && rs.ResultType == ResultType.Final);
        //            if (ResultSummaryFinal == null)
        //            {
        //                return new ResultSummary();
        //            }
        //        }
        //        return ResultSummaryFinal;
        //    }
        //}

        #endregion

        public void FinalizeSummaries()
        {
            CombineCalcAndManualSummaries();

            ResultSummaryFinal.UseOnReports = ResultSummaryFinal.BallotsNeedingReview == 0
                                              && ResultTies.All(rt => rt.IsResolved.AsBoolean())
                                              && ResultSummaryFinal.NumBallotsWithManual == ResultSummaryFinal.SumOfEnvelopesCollected;

            SaveChanges();
        }

        protected void DoAnalysisForTies()
        {

            // remove any results no longer needed
            Results.Where(r => r.VoteCount.AsInt() == 0).ToList().ForEach(r => RemoveResult(r));

            // remove any existing Tie info
            ResultTies.ForEach(rt => RemoveResultTie(rt));
            ResultTies.Clear();

            DetermineOrderAndSections();

            AnalyzeForTies();

        }

        /// <Summary>Assign an ordinal rank number to all results. Ties are NOT reflected in rank number. If there is a tie, they are sorted "randomly".</Summary>
        internal void DetermineOrderAndSections()
        {
            var election = TargetElection;

            var ordinalRank = 0;
            var ordinalRankInExtra = 0;

            // use RowId after VoteCount to ensure results are consistent when there is a tie in the VoteCount
            foreach (
                var result in
                    Results.OrderByDescending(r => r.VoteCount)
                           .ThenByDescending(r => r.TieBreakCount)
                           .ThenBy(r => r.C_RowId))
            {
                ordinalRank++;
                result.Rank = ordinalRank;

                DetermineSection(result, election, ordinalRank);

                if (result.Section == ResultHelper.Section.Extra)
                {
                    ordinalRankInExtra++;
                    result.RankInExtra = ordinalRankInExtra;
                }
            }
        }

        internal void AnalyzeForTies()
        {
            Result aboveResult = null;
            var nextTieBreakGroup = 1;

            foreach (var result in Results.OrderBy(r => r.Rank))
            {
                result.IsTied = false;
                result.TieBreakGroup = null;

                if (aboveResult != null)
                {
                    // compare this with the one 'above' it
                    var numFewerVotesThanAboveResult = aboveResult.VoteCount - result.VoteCount;
                    if (numFewerVotesThanAboveResult == 0)
                    {
                        aboveResult.IsTied = true;

                        result.IsTied = true;

                        if (aboveResult.TieBreakGroup.HasNoContent())
                        {
                            aboveResult.TieBreakGroup = nextTieBreakGroup;
                            nextTieBreakGroup++;
                        }
                        result.TieBreakGroup = aboveResult.TieBreakGroup;
                    }

                    // set CloseTo___ - if tied, then is also Close to
                    var isClose = numFewerVotesThanAboveResult <= ThresholdForCloseVote;
                    aboveResult.CloseToNext = isClose;
                    result.CloseToPrev = isClose;
                }
                else
                {
                    result.CloseToPrev = false;
                }

                aboveResult = result;
            }

            // last one
            if (aboveResult != null)
            {
                aboveResult.CloseToNext = false;
            }

            // pass 2
            for (var groupCode = 1; groupCode < nextTieBreakGroup; groupCode++)
            {
                var code = groupCode;

                var resultTie = new ResultTie
                    {
                        ElectionGuid = TargetElection.ElectionGuid,
                        TieBreakGroup = code
                    };

                ResultTies.Add(resultTie);
                AddResultTie(resultTie);

                AnalyzeTieGroup(resultTie, Results.Where(r => r.TieBreakGroup == code).OrderBy(r => r.Rank).ToList());
            }
        }

        private void AnalyzeTieGroup(ResultTie resultTie, List<Result> results)
        {
            AssertAtRuntime.That(results.Count != 0);

            resultTie.NumInTie = results.Count;

            resultTie.NumToElect = 0;
            resultTie.TieBreakRequired = false;

            var groupInTop = false;
            var groupInExtra = false;
            var groupInOther = false;

            foreach (var result in results)
            {
                switch (result.Section)
                {
                    case ResultHelper.Section.Top:
                        groupInTop = true;
                        break;
                    case ResultHelper.Section.Extra:
                        groupInExtra = true;
                        break;
                    case ResultHelper.Section.Other:
                        groupInOther = true;
                        break;
                }
            }
            var groupOnlyInTop = groupInTop && !(groupInExtra || groupInOther);
            var groupOnlyInOther = groupInOther && !(groupInTop || groupInExtra);

            results.ForEach(delegate(Result r)
                {
                    r.TieBreakRequired = !(groupOnlyInOther || groupOnlyInTop);
                    r.IsTieResolved = r.TieBreakCount.AsInt() > 0
                                      && !results.Any(r2 => r2.C_RowId != r.C_RowId
                                                            && r2.TieBreakCount == r.TieBreakCount);
                });

            if (groupInOther && (groupInTop || groupInExtra))
            {
                results.Where(r => r.Section == ResultHelper.Section.Other)
                       .ToList()
                       .ForEach(r => r.ForceShowInOther = true);
            }

            if (groupInTop)
            {
                if (!groupOnlyInTop)
                {
                    resultTie.NumToElect += results.Count(r => r.Section == ResultHelper.Section.Top);
                    resultTie.TieBreakRequired = true;
                    //resultTie.TieBreakRequired = results.Any(r => !r.IsTieResolved.AsBool());
                }
                else
                {
                    // default... tie-break not needed
                }
            }
            if (groupInExtra)
            {
                if (groupInTop && groupInOther || !groupInTop)
                {
                    resultTie.NumToElect += results.Count(r => r.Section == ResultHelper.Section.Extra);
                    resultTie.TieBreakRequired = true;
                    //resultTie.TieBreakRequired = results.Any(r => !r.IsTieResolved.AsBool());
                }
            }

            var foundBeforeDup = 0;
            if (resultTie.NumToElect > 0)
            {
                //results are in descending order already, so starting at 0 is starting at the "top"
                for (int i = 0, max = results.Count; i < max; i++)
                {
                    var result = results[i];
                    if (!result.IsTieResolved.AsBoolean()) break;
                    foundBeforeDup += result.TieBreakCount > 0 ? 1 : 0;
                }
            }

            if (foundBeforeDup < resultTie.NumToElect)
            {
                resultTie.IsResolved = false;
                results.ForEach(r => r.IsTieResolved = false);
            }
            else
            {
                resultTie.IsResolved = true;
                results.ForEach(r => r.IsTieResolved = true);
            }

            if (resultTie.NumInTie == resultTie.NumToElect)
            {
                resultTie.NumToElect--;
            }

            // conclusions
            //resultTie.Comments = resultTie.TieBreakRequired.AsBool() 
            //  ? "Tie-break required" 
            //  : "Tie-break not needed";
        }

        private static void DetermineSection(Result result, Election election, int rank)
        {
            string section;

            if (rank <= election.NumberToElect)
            {
                section = ResultHelper.Section.Top;
            }
            else if (rank <= (election.NumberToElect + election.NumberExtra))
            {
                section = ResultHelper.Section.Extra;
            }
            else
            {
                section = ResultHelper.Section.Other;
            }

            result.Section = section;
        }

        protected static void ResetValues(Result result)
        {
            result.CloseToNext = null;
            result.CloseToPrev = null;

            result.ForceShowInOther = false;
            result.IsTieResolved = null; // null, since only has meaning if IsTied

            result.IsTied = false; // not tied until proved otherwise

            result.Rank = -1;

            result.RankInExtra = null;

            result.Section = null;

            // result.TieBreakCount = null;  -- don't clear this, as it may be entered after tie-break vote is held

            result.TieBreakGroup = null;
            result.TieBreakRequired = false;

            result.VoteCount = null;
        }

        protected void FillCalcSummary()
        {
            ResultSummaryCalc.NumVoters = People.Count(p => p.VotingMethod.HasContent());
            ResultSummaryCalc.NumEligibleToVote =
                People.Count(p => !p.IneligibleReasonGuid.HasValue && p.CanVote.AsBoolean());

            ResultSummaryCalc.InPersonBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.InPerson);
            ResultSummaryCalc.MailedInBallots= People.Count(p => p.VotingMethod == VotingMethodEnum.MailedIn);
            ResultSummaryCalc.DroppedOffBallots= People.Count(p => p.VotingMethod == VotingMethodEnum.DroppedOff);
            ResultSummaryCalc.CalledInBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.CalledIn);
        }

        /// <summary>
        ///     Combine the automatic count with any values saved into a "Manual" result summary record
        /// </summary>
        public void CombineCalcAndManualSummaries()
        {
            var manualInput = ResultSummaries.FirstOrDefault(rs => rs.ResultType == ResultType.Manual)
                              ?? new ResultSummary();

            ResultSummaryFinal.BallotsNeedingReview = ResultSummaryCalc.BallotsNeedingReview;

            ResultSummaryFinal.BallotsReceived = manualInput.BallotsReceived.HasValue
                                                     ? manualInput.BallotsReceived.Value
                                                     : ResultSummaryCalc.BallotsReceived.GetValueOrDefault();

            ResultSummaryFinal.CalledInBallots = manualInput.CalledInBallots.HasValue
                                                     ? manualInput.CalledInBallots.Value
                                                     : ResultSummaryCalc.CalledInBallots.GetValueOrDefault();

            ResultSummaryFinal.DroppedOffBallots = manualInput.DroppedOffBallots.HasValue
                                                       ? manualInput.DroppedOffBallots.Value
                                                       : ResultSummaryCalc.DroppedOffBallots.GetValueOrDefault();

            ResultSummaryFinal.InPersonBallots = manualInput.InPersonBallots.HasValue
                                                     ? manualInput.InPersonBallots.Value
                                                     : ResultSummaryCalc.InPersonBallots.GetValueOrDefault();

            ResultSummaryFinal.MailedInBallots = manualInput.MailedInBallots.HasValue
                                                     ? manualInput.MailedInBallots.Value
                                                     : ResultSummaryCalc.MailedInBallots.GetValueOrDefault();

            ResultSummaryFinal.NumEligibleToVote = manualInput.NumEligibleToVote.HasValue
                                                       ? manualInput.NumEligibleToVote.Value
                                                       : ResultSummaryCalc.NumEligibleToVote.GetValueOrDefault();

            ResultSummaryFinal.NumVoters = manualInput.NumVoters.HasValue
                                               ? manualInput.NumVoters.Value
                                               : ResultSummaryCalc.NumVoters.GetValueOrDefault();

            ResultSummaryFinal.SpoiledManualBallots = manualInput.SpoiledManualBallots;

            // add manual to calculcated
            ResultSummaryFinal.SpoiledBallots = manualInput.SpoiledBallots.HasValue
                                        ? manualInput.SpoiledBallots.Value
                                        : ResultSummaryCalc.SpoiledBallots.GetValueOrDefault()
                                        + manualInput.SpoiledManualBallots.GetValueOrDefault();

            ResultSummaryFinal.SpoiledVotes = ResultSummaryCalc.SpoiledVotes;

            //ResultSummaryFinal.TotalVotes = manualInput.TotalVotes.HasValue
            //                                    ? manualInput.TotalVotes.Value
            //                                    : ResultSummaryCalc.TotalVotes.GetValueOrDefault();
        }
    }
}