using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ElectionAnalyzerSingleName : ElectionAnalyzerCore, IElectionAnalyzer
  {
    public ElectionAnalyzerSingleName()
    {
    }

    public ElectionAnalyzerSingleName(IAnalyzerFakes fakes)
      //, Election election,      List<VoteInfo> voteinfos, List<Ballot> ballots,      List<Person> people)
      : base(fakes)//, election, people, ballots, voteinfos)
    {
    }

    public ElectionAnalyzerSingleName(Election election, IStatusUpdateHub hub = null)
      : base(election, hub)
    {
    }

    //public ElectionAnalyzerSingleName(Election election, ResultSummary resultSummary, List<Result> results,
    //                                  List<VoteInfo> voteinfos, List<Ballot> ballots, Func<Result, Result> deleteResult,
    //                                  Func<Result, Result> addResult
    //                                  , Func<int> saveChanges, List<Person> people)
    //  : base(election, resultSummary, results, people, ballots, voteinfos, deleteResult, addResult, saveChanges)
    //{
    //}

    public override void AnalyzeEverything()
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

      _hub.StatusUpdate("Processing votes", true);
      var numDone = 0;

      // collect only valid votes
      foreach (var voteInfo in VoteInfos.Where(vi => vi.VoteStatusCode == VoteHelper.VoteStatusCode.Ok))
      {
        numDone++;
        if (numDone % 10 == 0)
        {
          _hub.StatusUpdate("Processed {0} vote{1}".FilledWith(numDone, numDone.Plural()), true);
        }

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
          //Savers.ResultSaver(DbAction.Add, result);
          Results.Add(result);
        }

        var voteCount = result.VoteCount.AsInt() + voteInfo.SingleNameElectionCount;
        result.VoteCount = voteCount;
      }
      _hub.StatusUpdate("Processed {0} unspoiled vote{1}".FilledWith(numDone, numDone.Plural()));

      FinalizeResultsAndTies();
      FinalizeSummaries();

      _hub.StatusUpdate("Saving");

      Db.SaveChanges();

      new ResultSummaryCacher(Db).DropThisCache();
    }
  }
}