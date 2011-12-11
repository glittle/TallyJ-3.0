using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class SingleNameElectionAnalyzer : ElectionAnalyzerCore
  {
    public SingleNameElectionAnalyzer()
    {
    }

    public SingleNameElectionAnalyzer(Election election, ResultSummary resultSummary, List<Result> results,
                                      List<vVoteInfo> voteinfos, Func<Result, Result> deleteResult,
                                      Func<Result, Result> addResult
                                      , Func<int> saveChanges, List<Person> people)
      : base(election, resultSummary, results, people, voteinfos, deleteResult, addResult, saveChanges)
    {
    }

    public override void GenerateResults()
    {
      // for single name elections, # votes = # ballots
      ResultSummaryAuto.BallotsReceived
        = ResultSummaryAuto.NumVoters
        = ResultSummaryAuto.TotalVotes
        = VoteInfos.Sum(vi => vi.SingleNameElectionCount).AsInt();

      var invalidBallotGuids =
        VoteInfos.Where(vi => vi.BallotStatusCode != "Ok").Select(ib => ib.BallotGuid).Distinct().ToList();

      ResultSummaryAuto.SpoiledBallots = invalidBallotGuids.Count();

      ResultSummaryAuto.SpoiledVotes = VoteInfos.Where(vi => !invalidBallotGuids.Contains(vi.BallotGuid) && IsNotValid(vi)).Sum(
          vi => vi.SingleNameElectionCount).AsInt();

      ResultSummaryAuto.NumEligibleToVote = People.Count(p => !p.IneligibleReasonGuid.HasValue && p.CanVote.AsBool());

      ResultSummaryAuto.BallotsNeedingReview = VoteInfos.Count(NeedReview);


      // clear any existing results
      Results.ForEach(ResetValues);

      // collect only valid votes
      foreach (var vVoteInfo in VoteInfos.Where(IsValid))
      {
        var voteInfo = vVoteInfo;

        // get existing result record for this person, if available
        var result =
          Results.SingleOrDefault(r => r.C_RowId == voteInfo.ResultId || r.PersonGuid == voteInfo.PersonGuid);
        if (result == null)
        {
          result = new Result
                     {
                       ElectionGuid = CurrentElection.ElectionGuid,
                       PersonGuid = voteInfo.PersonGuid.AsGuid()
                     };
          Results.Add(result);
          AddResult(result);
        }

        var voteCount = result.VoteCount.AsInt() + voteInfo.SingleNameElectionCount;
        result.VoteCount = voteCount;
      }

      // remove any results no longer needed
      foreach (var result in Results.Where(r => r.VoteCount.AsInt() == 0))
      {
        RemoveResult(result);
      }

      RankResults();

      AnalyzeForTies();

      SaveChanges();
    }

    private static void ResetValues(Result result)
    {
      result.CloseToNext = null;
      result.CloseToPrev = null;
      result.ForceShowInOther = null;
      result.IsTieResolved = null;
      result.IsTied = null;

      result.Rank = 0;
      result.RankInExtra = null;

      result.Section = null;

      result.TieBreakCount = null;
      result.TieBreakGroup = null;

      result.VoteCount = null;
    }
  }
}