using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public interface IAnalyzerFakes
  {
    ResultSummary ResultSummary { get; }
    Result RemoveResult(Result input);
    Result AddResult(Result arg);
    int SaveChanges();
    ResultTie RemoveResultTie(ResultTie arg);
    ResultTie AddResultTie(ResultTie arg);
  }

  public abstract class ElectionAnalyzerCore : DataConnectedModel, IElectionAnalyzer
  {
    private const int ThresholdForCloseVote = 3;

    private readonly Func<Result, Result> _addResult;
    private readonly Func<ResultTie, ResultTie> _addResultTie;
    private readonly Func<Result, Result> _deleteResult;
    private readonly Func<ResultTie, ResultTie> _deleteResultTie;
    private readonly Func<int> _saveChanges;
    private BallotAnalyzer _ballotAnalyzer;
    private List<Ballot> _ballots;
    private Election _election;
    private List<Person> _people;
    private ResultSummary _resultSummary;
    private List<ResultTie> _resultTies;
    private List<Result> _results;
    private List<vVoteInfo> _voteinfos;
    private List<Vote> _votes;

    protected ElectionAnalyzerCore()
    {
    }

    protected ElectionAnalyzerCore(IAnalyzerFakes fakes, Election election, List<Person> people, List<Ballot> ballots,
                                   List<vVoteInfo> voteinfos)
    {
      _election = election;
      _resultSummary = fakes.ResultSummary;
      _resultTies = new List<ResultTie>();
      _results = new List<Result>();
      _people = people;
      _ballots = ballots;
      _voteinfos = voteinfos;
      _votes = voteinfos.Select(vi => new Vote { C_RowId = vi.VoteId }).ToList();
      _deleteResult = fakes.RemoveResult;
      _addResult = fakes.AddResult;
      _saveChanges = fakes.SaveChanges;
      _deleteResultTie = fakes.RemoveResultTie;
      _addResultTie = fakes.AddResultTie;
    }

    protected ElectionAnalyzerCore(Election election)
    {
      _election = election;
    }

    protected BallotAnalyzer BallotAnalyzer
    {
      get { return _ballotAnalyzer ?? (_ballotAnalyzer = new BallotAnalyzer(TargetElection, SaveChanges)); }
    }

    /// <Summary>Remove this result from the datastore</Summary>
    protected Func<Result, Result> RemoveResult
    {
      get { return _deleteResult ?? Db.Results.Remove; }
    }

    /// <Summary>Remove this result from the datastore</Summary>
    protected Func<ResultTie, ResultTie> RemoveResultTie
    {
      get { return _deleteResultTie ?? Db.ResultTies.Remove; }
    }

    /// <Summary>Save all datastore changes</Summary>
    protected Func<int> SaveChanges
    {
      get { return _saveChanges ?? Db.SaveChanges; }
    }

    /// <Summary>Add this result to the datastore</Summary>
    protected Func<Result, Result> AddResult
    {
      get { return _addResult ?? Db.Results.Add; }
    }

    /// <Summary>Add this resultTie to the datastore</Summary>
    protected Func<ResultTie, ResultTie> AddResultTie
    {
      get { return _addResultTie ?? Db.ResultTies.Add; }
    }

    /// <Summary>Current Results records</Summary>
    public List<Person> People
    {
      get
      {
        return _people ?? (_people = Db.People
                                       .Where(p => p.ElectionGuid == TargetElection.ElectionGuid)
                                       .ToList());
      }
    }

    /// <Summary>Current Results records</Summary>
    public List<ResultTie> ResultTies
    {
      get
      {
        return _resultTies ?? (_resultTies = Db.ResultTies
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
        
        return _votes = Db.Votes.Where(v => voteIds.Contains(v.C_RowId)).ToList();
      }
    }

    #region IElectionAnalyzer Members

    public List<Ballot> Ballots
    {
      get
      {
        return _ballots ?? (_ballots = Db.Ballots
                                         .Where(
                                           b =>
                                           Db.Locations.Where(l => l.ElectionGuid == TargetElection.ElectionGuid).
                                             Select(l => l.LocationGuid).Contains(b.LocationGuid))
                                         .ToList());
      }
    }

    /// <Summary>Current Results records</Summary>
    public List<Result> Results
    {
      get
      {
        return _results ?? (_results = Db.Results
                                         .Where(r => r.ElectionGuid == TargetElection.ElectionGuid)
                                         .ToList());
      }
    }

    /// <Summary>Current Results records</Summary>
    public ResultSummary ResultSummaryAuto
    {
      get
      {
        if (_resultSummary != null)
        {
          return _resultSummary;
        }

        _resultSummary = Db.ResultSummaries.SingleOrDefault(rs => rs.ElectionGuid == TargetElection.ElectionGuid);

        if (_resultSummary == null)
        {
          _resultSummary = new ResultSummary
            {
              ElectionGuid = TargetElection.ElectionGuid,
              ResultType = ResultType.Automatic
            };
          Db.ResultSummaries.Add(_resultSummary);
        }

        return _resultSummary;
      }
    }

    /// <Summary>Current VoteInfo records. They are detached, so no updates can be done</Summary>
    public List<vVoteInfo> VoteInfos
    {
      get
      {
        if (_voteinfos != null) return _voteinfos;
        else
          _voteinfos = Db.vVoteInfoes
            .Where(vi => vi.ElectionGuid == TargetElection.ElectionGuid)
            .OrderBy(vi => vi.BallotGuid)
            .ToList();
        _voteinfos.ForEach(Db.Detach);
        return _voteinfos;
      }
    }

    /// <Summary>Check locally and in DB to see if the result is available at the moment</Summary>
    public bool IsResultAvailable
    {
      get
      {
        if (_resultSummary != null)
        {
          return true;
        }

        _resultSummary = Db.ResultSummaries.SingleOrDefault(rs => rs.ElectionGuid == TargetElection.ElectionGuid);

        return _resultSummary != null;
      }
    }

    /// <Summary>In the Core, do some common results generation</Summary>
    public virtual ResultSummary GenerateResults()
    {
      // first refresh all votes and ballots
      if (VoteAnalyzer.UpdateAllStatuses(VoteInfos, Votes))
      {
        SaveChanges();
      }
      BallotAnalyzer.UpdateAllBallotStatuses(Ballots, VoteInfos);

      // clear any existing results
      Results.ForEach(ResetValues);

      var summary = ResultSummaryAuto;

      UpdateSummaryFromPeopleRecords(summary);

      return summary;
    }

    #endregion

    protected void DoFinalAnalysis()
    {
      // remove any results no longer needed
      Results.Where(r => r.VoteCount.AsInt() == 0).ToList().ForEach(r => RemoveResult(r));

      // remove any existing Tie info
      ResultTies.ForEach(rt => RemoveResultTie(rt));

      DetermineOrderAndSections();

      AnalyzeForTies();

      SaveChanges();
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
          Results.OrderByDescending(r => r.VoteCount).ThenByDescending(r => r.TieBreakCount).ThenBy(r => r.C_RowId))
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
      var nextTieBreakGroup = 'A';

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
              aboveResult.TieBreakGroup = "" + nextTieBreakGroup;
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
      for (var groupCode = 'A'; groupCode < nextTieBreakGroup; groupCode++)
      {
        var code = "" + groupCode;

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
        results.Where(r => r.Section == ResultHelper.Section.Other).ToList().ForEach(r => r.ForceShowInOther = true);
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

    protected void UpdateSummaryFromPeopleRecords(ResultSummary summary)
    {
      summary.NumEligibleToVote = People.Count(p => !p.IneligibleReasonGuid.HasValue && p.CanVote.AsBoolean());
      summary.NumVoters = People.Count(p => p.VotingMethod.HasContent());

      summary.InPersonBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.InPerson);
      summary.MailedInBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.MailedIn);
      summary.DroppedOffBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.DroppedOff);
      summary.CalledInBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.CalledIn);
    }
  }
}