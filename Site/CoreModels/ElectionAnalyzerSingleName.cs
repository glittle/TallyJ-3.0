using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Models;

namespace TallyJ.CoreModels
{
  public class ElectionAnalyzerSingleName : ElectionAnalyzerCore
  {
    public ElectionAnalyzerSingleName()
    {
    }

    public ElectionAnalyzerSingleName(IAnalyzerFakes fakes, Election election, 
                                  List<vVoteInfo> voteinfos, List<Ballot> ballots,
                                  List<Person> people)
      : base(fakes, election, people, ballots, voteinfos)
    {
    }

    public ElectionAnalyzerSingleName(Election election) : base(election)
    {
    }

    //public ElectionAnalyzerSingleName(Election election, ResultSummary resultSummary, List<Result> results,
    //                                  List<vVoteInfo> voteinfos, List<Ballot> ballots, Func<Result, Result> deleteResult,
    //                                  Func<Result, Result> addResult
    //                                  , Func<int> saveChanges, List<Person> people)
    //  : base(election, resultSummary, results, people, ballots, voteinfos, deleteResult, addResult, saveChanges)
    //{
    //}

    public override ResultSummary GenerateResults()
    {
      var summary = base.GenerateResults();
      
      // for single name elections, # votes = # ballots
      summary.BallotsReceived
        = summary.NumVoters
        = summary.TotalVotes
        = VoteInfos.Sum(vi => vi.SingleNameElectionCount).AsInt();

      var invalidBallotGuids = Ballots.Where(bi => bi.StatusCode != BallotStatusEnum.Ok).Select(ib => ib.BallotGuid).ToList();
        //VoteInfos.Where(vi => vi.BallotStatusCode != "Ok").Select(ib => ib.BallotGuid).Distinct().ToList();

      summary.SpoiledBallots = invalidBallotGuids.Count();

      summary.SpoiledVotes = VoteInfos.Where(vi => !invalidBallotGuids.Contains(vi.BallotGuid) && VoteAnalyzer.IsNotValid(vi)).Sum(
          vi => vi.SingleNameElectionCount).AsInt();

      summary.NumEligibleToVote = People.Count(p => !p.IneligibleReasonGuid.HasValue && p.CanVote.AsBoolean());

      summary.BallotsNeedingReview = VoteInfos.Count(VoteAnalyzer.VoteNeedReview);


      // clear any existing results
      Results.ForEach(ResetValues);

      // collect only valid votes
      foreach (var vVoteInfo in VoteInfos.Where(VoteAnalyzer.VoteIsValid))
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

        var voteCount = result.VoteCount.AsInt() + voteInfo.SingleNameElectionCount;
        result.VoteCount = voteCount;
      }

      DoFinalAnalysis(summary);

      return summary;
    }
  }
}