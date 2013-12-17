using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.EF;

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
      ResultSummaryCalc.TotalVotes = ResultSummaryCalc.BallotsReceived * TargetElection.NumberToElect;

      var invalidBallotGuids = Ballots.Where(b => b.StatusCode != BallotStatusEnum.Ok).Select(b => b.BallotGuid).ToList();

      ResultSummaryCalc.SpoiledBallots = invalidBallotGuids.Count();
      ResultSummaryCalc.SpoiledVotes =
        VoteInfos.Count(vi => !invalidBallotGuids.Contains(vi.BallotGuid) && VoteAnalyzer.IsNotValid(vi));

      var electionGuid = TargetElection.ElectionGuid;

      // collect only valid ballots
      foreach (var ballot in Ballots.Where(bi => bi.StatusCode == BallotStatusEnum.Ok))
      {
        var ballotGuid = ballot.BallotGuid;
        // collect only valid votes
        foreach (var voteInfoRaw in VoteInfos.Where(vi => vi.BallotGuid == ballotGuid && VoteAnalyzer.VoteIsValid(vi)))
        {
          var voteInfo = voteInfoRaw;

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
            
            ResultSaver(DbAction.Add, result);
          }

          var voteCount = result.VoteCount.AsInt() + 1;
          result.VoteCount = voteCount;
        }
      }

      FinalizeResultsAndTies();
      FinalizeSummaries();

      return ResultSummaryFinal;
    }
  }
}