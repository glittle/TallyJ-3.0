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
                                      , Func<int> saveChanges)
      : base(election, resultSummary, results, voteinfos, deleteResult, addResult, saveChanges)
    {
    }

    public override void GenerateResults()
    {
      var numVotes = VoteInfos.Count();
      var totalInvalid = VoteInfos.Count(IsNotValid);
      var needReview = VoteInfos.Count(vi => vi.PersonRowVersion != vi.PersonRowVersionInVote);

      // clear results
      Results.ForEach(ResetValues);

      // collect only valid votes
      foreach (var vVoteInfo in VoteInfos.Where(IsValid))
      {
        var voteInfo = vVoteInfo;

        // get existing result record for this person, if available
        var result =
          Results.SingleOrDefault(r => r.C_RowId == voteInfo.ResultRowId || r.PersonGuid == voteInfo.PersonGuid);
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