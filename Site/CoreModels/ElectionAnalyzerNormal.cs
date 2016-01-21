using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.EF;
using EntityFramework.BulkInsert.Extensions;

namespace TallyJ.CoreModels
{
  public class ElectionAnalyzerNormal : ElectionAnalyzerCore, IElectionAnalyzer
  {
    public ElectionAnalyzerNormal()
    {
    }

    public ElectionAnalyzerNormal(IAnalyzerFakes fakes, Election election,
      List<VoteInfo> voteinfos, List<Ballot> ballots,
      List<Person> people)
      : base(fakes, election, people, ballots, voteinfos)
    {
    }

    public ElectionAnalyzerNormal(Election election)
      : base(election)
    {
    }

    public override ResultSummary AnalyzeEverything()
    {
      PrepareForAnalysis();

      ResultSummaryCalc.BallotsNeedingReview = Ballots.Count(BallotAnalyzer.BallotNeedsReview);

      ResultSummaryCalc.BallotsReceived = Ballots.Count;
      ResultSummaryCalc.TotalVotes = ResultSummaryCalc.BallotsReceived*TargetElection.NumberToElect;

      var invalidBallotGuids =
        Ballots.Where(b => b.StatusCode != BallotStatusEnum.Ok).Select(b => b.BallotGuid).ToList();

      ResultSummaryCalc.SpoiledBallots = invalidBallotGuids.Count();
      ResultSummaryCalc.SpoiledVotes =
        VoteInfos.Count(
          vi => !invalidBallotGuids.Contains(vi.BallotGuid) && vi.VoteStatusCode != VoteHelper.VoteStatusCode.Ok);

      var electionGuid = TargetElection.ElectionGuid;

      // collect only valid ballots
      _hub.LoadStatus("Counting ballots", true);
      var numDone = 0;
      foreach (var ballot in Ballots.Where(bi => bi.StatusCode == BallotStatusEnum.Ok))
      {
        numDone++;
        if (numDone % 10 == 0) {
          _hub.LoadStatus("Counted {0} ballot{1}".FilledWith(numDone, numDone.Plural()), true);
        }

        var ballotGuid = ballot.BallotGuid;
        // collect only valid votes
        foreach (
          var voteInfoRaw in
            VoteInfos.Where(vi => vi.BallotGuid == ballotGuid && vi.VoteStatusCode == VoteHelper.VoteStatusCode.Ok))
        {
          var voteInfo = voteInfoRaw;

          // get existing result record for this person, if available
          var result = Results.FirstOrDefault(r => r.ElectionGuid == electionGuid && r.PersonGuid == voteInfo.PersonGuid);
          //Result result = null;
          //if (results.Count == 1)
          //{
          //  result = results[0];
          //}
          //else if (results.Count > 1) {
          //  // old/bad data!
          //  foreach (var r in results)
          //  {
          //    Savers.ResultSaver(DbAction.AttachAndRemove, r);
          //    Results.Remove(r);
          //  }
          //}
          if (result == null)
          {
            result = new Result
            {
              ElectionGuid = electionGuid,
              PersonGuid = voteInfo.PersonGuid.AsGuid()
            };
            InitializeSomeProperties(result);

            Savers.ResultSaver(DbAction.Add, result);
            //Results.Add(result);
          }

          var voteCount = result.VoteCount.AsInt() + 1;
          result.VoteCount = voteCount;
        }
      }
      _hub.LoadStatus("Counted {0} unspoiled ballot{1}".FilledWith(numDone, numDone.Plural()));

      FinalizeResultsAndTies();
      FinalizeSummaries();

      SaveChanges();

      return ResultSummaryFinal;
    }
  }
}