using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ElectionAnalyzerSingleName : ElectionAnalyzerCore, IElectionAnalyzer
  {
    public ElectionAnalyzerSingleName()
    {
    }

    public ElectionAnalyzerSingleName(IAnalyzerFakes fakes, Election election,
      List<VoteInfo> voteinfos, List<Ballot> ballots,
      List<Person> people)
      : base(fakes, election, people, ballots, voteinfos)
    {
    }

    public ElectionAnalyzerSingleName(Election election)
      : base(election)
    {
    }

    //public ElectionAnalyzerSingleName(Election election, ResultSummary resultSummary, List<Result> results,
    //                                  List<VoteInfo> voteinfos, List<Ballot> ballots, Func<Result, Result> deleteResult,
    //                                  Func<Result, Result> addResult
    //                                  , Func<int> saveChanges, List<Person> people)
    //  : base(election, resultSummary, results, people, ballots, voteinfos, deleteResult, addResult, saveChanges)
    //{
    //}

    public override ResultSummary AnalyzeEverything()
    {

      PrepareForAnalysis();

      // for single name elections, # votes = # ballots
      ResultSummaryCalc.BallotsReceived
        = ResultSummaryCalc.NumVoters
          = ResultSummaryCalc.TotalVotes
            = VoteInfos.Sum(vi => vi.SingleNameElectionCount).AsInt();

      var invalidBallotGuids =
        Ballots.Where(bi => bi.StatusCode != BallotStatusEnum.Ok).Select(ib => ib.BallotGuid).ToList();
      //VoteInfos.Where(vi => vi.BallotStatusCode != "Ok").Select(ib => ib.BallotGuid).Distinct().ToList();

      ResultSummaryCalc.SpoiledBallots = invalidBallotGuids.Count();

      ResultSummaryCalc.SpoiledVotes =
        VoteInfos.Where(vi => !invalidBallotGuids.Contains(vi.BallotGuid) && vi.VoteStatusCode!=VoteHelper.VoteStatusCode.Ok).Sum(
          vi => vi.SingleNameElectionCount).AsInt();

      // vote == ballot for this election
      ResultSummaryCalc.BallotsNeedingReview = VoteInfos.Count(VoteAnalyzer.VoteNeedReview);

      // clear any existing results
      Results.ForEach(InitializeSomeProperties);

      var electionGuid = TargetElection.ElectionGuid;

      // collect only valid votes
      foreach (var voteInfo in VoteInfos.Where(vi => vi.VoteStatusCode == VoteHelper.VoteStatusCode.Ok))
      {
        // get existing result record for this person, if available
        var result =
          Results.SingleOrDefault(r => r.ElectionGuid == electionGuid && r.PersonGuid == voteInfo.PersonGuid);
        if (result == null)
        {
          result = new Result
          {
            ElectionGuid = electionGuid,
            PersonGuid = voteInfo.PersonGuid.AsGuid()
          };
          InitializeSomeProperties(result);
          _savers.ResultSaver(DbAction.Add, result);
        }

        var voteCount = result.VoteCount.AsInt() + voteInfo.SingleNameElectionCount;
        result.VoteCount = voteCount;
      }

      FinalizeResultsAndTies();
      FinalizeSummaries();

      return ResultSummaryFinal;
    }
  }
}