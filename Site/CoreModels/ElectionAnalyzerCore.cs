using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Linq.Dynamic;
using EntityFramework.Extensions;
using Microsoft.Owin.Security.Provider;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public enum DbAction
  {
    Add,
    Attach,
    Save,
    AttachAndRemove
  }

  public abstract class ElectionAnalyzerCore : DataConnectedModel
  {
    private const int ThresholdForCloseVote = 3;

    private readonly Func<int> _saveChanges;
    private List<Ballot> _ballots;
    private Election _election;
    private List<Person> _people;
    private List<ResultSummary> _resultSummaries;
    private List<ResultTie> _resultTies;
    private List<Result> _results;
    private List<VoteInfo> _voteinfos;
    private List<Vote> _votes;
    protected readonly Savers Savers;

    protected ElectionAnalyzerCore()
    {
      Savers = new Savers();
    }

    protected ElectionAnalyzerCore(IAnalyzerFakes fakes, Election election, List<Person> people,
                                   List<Ballot> ballots,
                                   List<VoteInfo> voteinfos)
    {
      Savers = new Savers(fakes);
      _election = election;
      _resultTies = fakes.ResultTies;
      _results = fakes.Results;
      _resultSummaries = fakes.ResultSummaries;
      _resultSummaries.Add(fakes.ResultSummaryManual);
      _people = people;
      _ballots = ballots;
      _voteinfos = voteinfos;
      _votes = voteinfos.Select(vi => new Vote { C_RowId = vi.VoteId }).ToList();
      _saveChanges = fakes.SaveChanges;
      IsFaked = true;
    }

    public bool IsFaked { get; private set; }

    protected ElectionAnalyzerCore(Election election)
    {
      _election = election;
      Savers = new Savers();
    }

    public ResultSummary ResultSummaryCalc { get; private set; }
    public ResultSummary ResultSummaryFinal { get; private set; }
    public ResultSummary ResultSummaryManual { get; private set; }

    /// <Summary>Remove this result from the datastore</Summary>
    //protected Func<Result, Result> RemoveResult
    //{
    //  get { return _deleteResult ?? Db.Result.Remove; }
    //}

    ///// <Summary>Remove this result from the datastore</Summary>
    //protected Func<ResultTie, ResultTie> RemoveResultTie
    //{
    //  get { return _deleteResultTie ?? ResultTieSaver(DbAction.AttachAndRemove,  Db.ResultTie.Remove; }
    //}

    /// <Summary>Save all datastore changes</Summary>
    protected Func<int> SaveChanges
    {
      get { return _saveChanges ?? Db.SaveChanges; }
    }

    //
    //    protected void VoteSaver(DbAction action, Vote vote)
    //    {
    //      switch (action)
    //      {
    //        case DbAction.Attach:
    //          if (!IsFaked)
    //          {
    //            Db.Vote.Attach(vote);
    //          }
    //          break;
    //
    //        case DbAction.Save:
    //          if (!IsFaked)
    //          {
    //            new VoteCacher().UpdateItemAndSaveCache(vote);
    //          }
    //          break;
    //
    //        default:
    //          throw new ArgumentOutOfRangeException("action");
    //      }
    //    }
    //
    //
    //    protected void BallotSaver(DbAction action, Ballot ballot)
    //    {
    //      switch (action)
    //      {
    //        case DbAction.Attach:
    //          if (!IsFaked)
    //          {
    //            Db.Ballot.Attach(ballot);
    //          }
    //          break;
    //
    //        case DbAction.Save:
    //          if (!IsFaked)
    //          {
    //            new BallotCacher().UpdateItemAndSaveCache(ballot);
    //          }
    //          break;
    //
    //        default:
    //          throw new ArgumentOutOfRangeException("action");
    //      }
    //    }
    //
    //    protected void ResultSaver(DbAction action, Result result)
    //    {
    //      switch (action)
    //      {
    //        case DbAction.Add:
    //          result.C_RowId = tempRowId--;
    //          if (!IsFaked)
    //          {
    //            Db.Result.Add(result);
    //            new ResultCacher().UpdateItemAndSaveCache(result);
    //          }
    //          else
    //          {
    //            _addResult(result);
    //          }
    //          break;
    //
    //        case DbAction.Attach:
    //          if (!IsFaked)
    //          {
    //            Db.Result.Attach(result);
    //          }
    //          break;
    //
    //        case DbAction.Save:
    //          if (!IsFaked)
    //          {
    //            new ResultCacher().UpdateItemAndSaveCache(result);
    //          }
    //          break;
    //
    //        case DbAction.AttachAndRemove:
    //          if (!IsFaked)
    //          {
    //            new ResultCacher().RemoveItemAndSaveCache(result);
    //            Db.Result.Attach(result);
    //            Db.Result.Remove(result);
    //          }
    //          break;
    //
    //        default:
    //          throw new ArgumentOutOfRangeException("action");
    //      }
    //    }
    //
    //    /// <Summary>Add this result to the datastore</Summary>
    ////    protected void AddResultSummary(ResultSummary resultSummary)
    ////    {
    ////      ResultSummaries.Add(resultSummary);
    ////      if (_addResultSummary != null)
    ////      {
    ////        _addResultSummary(resultSummary);
    ////      }
    ////      else
    ////      {
    ////        resultSummary.C_RowId = tempRowId--;
    ////        Db.ResultSummary.Add(resultSummary);
    ////        new ResultSummaryCacher().UpdateItemAndSaveCache(resultSummary);
    ////      }
    ////    }
    //
    //
    //    protected void ResultSummarySaver(DbAction action, ResultSummary resultSummary)
    //    {
    //      switch (action)
    //      {
    //        case DbAction.Add:
    //          resultSummary.C_RowId = tempRowId--;
    //          if (!IsFaked)
    //          {
    //            Db.ResultSummary.Add(resultSummary);
    //            new ResultSummaryCacher().UpdateItemAndSaveCache(resultSummary);
    //          }
    //          else
    //          {
    //            _addResultSummary(resultSummary);
    //          }
    //          break;
    //
    //        case DbAction.Attach:
    //          if (!IsFaked)
    //          {
    //            Db.ResultSummary.Attach(resultSummary);
    //          }
    //          break;
    //
    //        case DbAction.Save:
    //          if (!IsFaked)
    //          {
    //            new ResultSummaryCacher().UpdateItemAndSaveCache(resultSummary);
    //          }
    //          break;
    //
    //        case DbAction.AttachAndRemove:
    //          if (!IsFaked)
    //          {
    //            new ResultSummaryCacher().RemoveItemAndSaveCache(resultSummary);
    //            Db.ResultSummary.Attach(resultSummary);
    //            Db.ResultSummary.Remove(resultSummary);
    //          }
    //          break;
    //
    //        default:
    //          throw new ArgumentOutOfRangeException("action");
    //      }
    //    }
    //
    //    /// <Summary>Add this resultTie to the datastore</Summary>
    //    //protected Func<ResultTie, ResultTie> AddResultTie
    //    //{
    //    //  get { return _addResultTie ?? Db.ResultTie.Add; }
    //    //}
    //
    //    protected void ResultTieSaver(DbAction action, ResultTie resultTie)
    //    {
    //      switch (action)
    //      {
    //        case DbAction.Add:
    //          if (!IsFaked)
    //          {
    //            resultTie.C_RowId = tempRowId--;
    //            Db.ResultTie.Add(resultTie);
    //            new ResultTieCacher().UpdateItemAndSaveCache(resultTie);
    //          }
    //          else
    //          {
    //            _addResultTie(resultTie);
    //          }
    //          break;
    //
    //        case DbAction.Attach:
    //          if (!IsFaked)
    //          {
    //            Db.ResultTie.Attach(resultTie);
    //          }
    //          break;
    //
    //        case DbAction.Save:
    //          if (!IsFaked)
    //          {
    //            new ResultTieCacher().UpdateItemAndSaveCache(resultTie);
    //          }
    //          break;
    //
    //        case DbAction.AttachAndRemove:
    //          if (!IsFaked && resultTie.C_RowId > 0)
    //          {
    //            new ResultTieCacher().RemoveItemAndSaveCache(resultTie);
    //            Db.ResultTie.Attach(resultTie);
    //            Db.ResultTie.Remove(resultTie);
    //          }
    //          break;
    //
    //        default:
    //          throw new ArgumentOutOfRangeException("action");
    //      }
    //    }


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
        return _resultTies ?? (_resultTies = new ResultTieCacher().AllForThisElection);
      }
    }

    internal Election TargetElection
    {
      get { return _election ?? (_election = UserSession.CurrentElection); }
    }

    /// <Summary>Votes are loaded, in case DB updates are required.</Summary>
    public List<Vote> Votes
    {
      get { return _votes ?? (_votes = new VoteCacher().AllForThisElection); }
    }

    #region IElectionAnalyzer Members

    public List<Ballot> Ballots
    {
      get { return _ballots ?? (_ballots = new BallotCacher().AllForThisElection); }
    }

    /// <Summary>Current Results records</Summary>
    public List<Result> Results
    {
      get { return _results ?? (_results = new ResultCacher().AllForThisElection); }
    }

    public List<ResultSummary> ResultSummaries
    {
      get { return _resultSummaries ?? (_resultSummaries = new ResultSummaryCacher().AllForThisElection); }
    }

    /// <Summary>Current VoteInfo records. They are detached, so no updates can be done</Summary>
    public List<VoteInfo> VoteInfos
    {
      get
      {
        if (_voteinfos != null) return _voteinfos;

        return _voteinfos = new VoteCacher().AllForThisElection
          .JoinMatchingOrNull(new PersonCacher().AllForThisElection, v => v.PersonGuid, p => p.PersonGuid, (v, p) => new { v, p })
          .Select(g => new VoteInfo(g.v, TargetElection, new BallotCacher().AllForThisElection.Single(b => b.BallotGuid == g.v.BallotGuid), UserSession.CurrentLocation, g.p))
          .OrderBy(vi => vi.PositionOnBallot)
          .ToList();
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

        ResultSummaryFinal = new ResultSummaryCacher().AllForThisElection
          .FirstOrDefault(rs => rs.ResultType == ResultType.Final);

        return ResultSummaryFinal != null;
      }
    }

    /// <Summary>In the Core, do some common results generation</Summary>
    public abstract ResultSummary AnalyzeEverything();

    public void PrepareForAnalysis()
    {
      var electionGuid = TargetElection.ElectionGuid;
      if (!IsFaked)
      {
        var resultSummaryCacher = new ResultSummaryCacher();
        var summaries = resultSummaryCacher.AllForThisElection;
        resultSummaryCacher.RemoveItemsAndSaveCache(summaries.Where(rs => rs.ResultType != ResultType.Manual));

        Db.ResultSummary.Delete(r => r.ElectionGuid == electionGuid && r.ResultType != ResultType.Manual);
      }

      // first refresh all votes
      VoteAnalyzer.UpdateAllStatuses(VoteInfos, Votes, Savers.VoteSaver);

      // then refresh all ballots
      var ballotAnalyzer = new BallotAnalyzer(TargetElection, Savers.BallotSaver);
      ballotAnalyzer.UpdateAllBallotStatuses(Ballots, VoteInfos);

      // attach results, but don't save yet
      Results.ForEach(delegate(Result result)
      {
        Savers.ResultSaver(DbAction.Attach, result);
        InitializeSomeProperties(result);
      });

      PrepareResultSummaries();

      FillResultSummaryCalc();
    }

    /// <summary>
    ///     Load the Calc and Final summaries
    /// </summary>
    public void PrepareResultSummaries()
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

          Savers.ResultSummarySaver(DbAction.Add, ResultSummaryCalc);
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
          Savers.ResultSummarySaver(DbAction.Add, ResultSummaryFinal);
        }
      }
    }

    #endregion

    public void FinalizeSummaries()
    {
      CombineCalcAndManualSummaries();

      ResultSummaryFinal.UseOnReports = ResultSummaryFinal.BallotsNeedingReview == 0
                                        && ResultTies.All(rt => rt.IsResolved.AsBoolean())
                                        && ResultSummaryFinal.NumBallotsWithManual == ResultSummaryFinal.SumOfEnvelopesCollected;

      SaveChanges();
    }

    protected void FinalizeResultsAndTies()
    {

      // remove any results no longer needed
      Results.Where(r => r.VoteCount.AsInt() == 0).ToList().ForEach(r => Savers.ResultSaver(DbAction.AttachAndRemove, r));

      // remove any existing Tie info
      if (!IsFaked)
      {
        Db.ResultTie.Delete(rt => rt.ElectionGuid == _election.ElectionGuid);
        new ResultTieCacher().DropThisCache();
        ResultTies.Clear();
      }
      else
      {
        ResultTies.Clear();
      }

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
              TieBreakGroup = code,
            };

        Savers.ResultTieSaver(DbAction.Add, resultTie);

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
                                                    && r2.TieBreakCount == r.TieBreakCount
                                                    && (r2.Section != r.Section || r.Section == ResultHelper.Section.Extra));
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
        }
        else
        {
          // default... tie-break not needed
        }
      }
      if (groupInExtra)
      {
        if (groupInTop)
        {
          //resultTie.NumToElect += results.Count(r => r.Section == ResultHelper.Section.Extra);
          resultTie.TieBreakRequired = true;
          //resultTie.TieBreakRequired = results.Any(r => !r.IsTieResolved.AsBool());
        }
        else
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

    protected static void InitializeSomeProperties(Result result)
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

    protected void FillResultSummaryCalc()
    {
      ResultSummaryCalc.NumVoters = People.Count(p => p.VotingMethod.HasContent());
      ResultSummaryCalc.NumEligibleToVote =
          People.Count(p => !p.IneligibleReasonGuid.HasValue && p.CanVote.AsBoolean());

      ResultSummaryCalc.InPersonBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.InPerson);
      ResultSummaryCalc.MailedInBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.MailedIn);
      ResultSummaryCalc.DroppedOffBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.DroppedOff);
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