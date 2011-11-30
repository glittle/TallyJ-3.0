using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class ElectionAnalyzerCore : DataConnectedModel, IElectionAnalyzer
  {
    private readonly Func<Result, Result> _deleteResult;
    private Election _election;
    private ResultSummary _resultSummary;
    private List<Result> _results;
    private List<vVoteInfo> _voteinfos;
    private readonly Func<Result, Result> _addResult;
    private Func<int> _saveChanges;

    public ElectionAnalyzerCore()
    {
    }

    public ElectionAnalyzerCore(Election election, ResultSummary resultSummary, List<Result> results,
                                List<vVoteInfo> voteinfos, Func<Result, Result> deleteResult, Func<Result, Result> addResult, Func<int> saveChanges)
    {
      _election = election;
      _resultSummary = resultSummary;
      _results = results;
      _voteinfos = voteinfos;
      _deleteResult = deleteResult;
      _addResult = addResult;
      _saveChanges = saveChanges;
    }

    /// <Summary>Remove this result from the datastore</Summary>
    protected Func<Result, Result> RemoveResult
    {
      get { return _deleteResult ?? Db.Results.Remove; }
    }
    
    /// <Summary>Remove this result from the datastore</Summary>
    protected Func<int> SaveChanges
    {
      get { return _saveChanges ?? Db.SaveChanges; }
    }
    
    /// <Summary>Remove this result from the datastore</Summary>
    protected Func<Result, Result> AddResult
    {
      get { return _addResult ?? Db.Results.Add; }
    }

    /// <Summary>Current Results records</Summary>
    public List<Result> Results
    {
      get
      {
        return _results ?? (_results = Db.Results
                                         .Where(r => r.ElectionGuid == CurrentElection.ElectionGuid)
                                         .ToList());
      }
    }

    internal Election CurrentElection
    {
      get { return _election ?? (_election = UserSession.CurrentElection); }
    }

    /// <Summary>Current Results records</Summary>
    public ResultSummary ResultSummaryAuto
    {
      get
      {
        return _resultSummary ?? (_resultSummary = Db.ResultSummaries
                                                     .SingleOrDefault(
                                                       r => r.ElectionGuid == CurrentElection.ElectionGuid) ??
                                                   new ResultSummary
                                                     {ElectionGuid = CurrentElection.ElectionGuid, ResultType = "A"});
      }
    }

    /// <Summary>Current VoteInfo records</Summary>
    public List<vVoteInfo> VoteInfos
    {
      get
      {
        return _voteinfos ?? (_voteinfos = Db.vVoteInfoes
                                             .Where(vi => vi.ElectionGuid == CurrentElection.ElectionGuid)
                                             .OrderBy(vi => vi.BallotGuid)
                                             .ToList());
      }
    }

    #region IElectionAnalyzer Members

    public virtual void GenerateResults()
    {
      throw new NotImplementedException();
    }

    #endregion

    /// <Summary>Is this Vote not valid?</Summary>
    public static bool IsNotValid(vVoteInfo voteInfo)
    {
      return !IsValid(voteInfo);
    }

    /// <Summary>Is this Vote valid?</Summary>
    public static bool IsValid(vVoteInfo voteInfo)
    {
      return !voteInfo.InvalidReasonGuid.HasValue
             && !voteInfo.PersonIneligibleReasonGuid.HasValue
             && voteInfo.BallotStatusCode == BallotHelper.BallotStatusCode.Ok
             && voteInfo.VoteStatusCode == BallotHelper.VoteStatusCode.Ok
             && voteInfo.PersonRowVersion == voteInfo.PersonRowVersionInVote;
    }


    internal void RankResults()
    {
      var election = CurrentElection;

      var rank = 0;
      var rankInExtra = 0;

      Result lastResult = null;

      foreach (var result in Results.OrderByDescending(r => r.VoteCount))
      {
        if (lastResult == null || lastResult.VoteCount != result.VoteCount)
        {
          rank++;
        }
        result.Rank = rank;
        if (result.Section == Section.Extra)
        {
          if (rankInExtra == 0 || lastResult.VoteCount != result.VoteCount)
          {
            rankInExtra++;
          }

          result.RankInExtra = rankInExtra;
        }

        DetermineSection(result, election, rank);

        lastResult = result;
      }
    }

    internal void AnalyzeForTies()
    {
      Result lastResult = null;
      foreach (var result in Results.OrderBy(r => r.Rank))
      {
        if (lastResult != null)
        {
          // compare this with the one 'above' it
          if (result.VoteCount == lastResult.VoteCount)
          {
            lastResult.IsTied = true;
            result.IsTied = true;

            if (lastResult.TieBreakGroup.HasNoContent())
            {
              lastResult.TieBreakGroup = "A";
            }
            result.TieBreakGroup = lastResult.TieBreakGroup;
          }
        }


        lastResult = result;
      }
    }

    private static void DetermineSection(Result result, Election election, int rank)
    {
      string section;
      if (rank <= election.NumberToElect)
      {
        section = Section.Top;
      }
      else if (rank <= (election.NumberToElect + election.NumberExtra))
      {
        section = Section.Extra;
      }
      else
      {
        section = Section.Other;
      }

      result.Section = section;
    }
  }

  public static class Section
  {
    public const string Top = "A";
    public const string Extra = "B";
    public const string Other = "C";
  }
}