//using Microsoft.Owin.Security.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;
using TallyJ.CoreModels.Hubs;
using TallyJ.Code.UnityRelated;
using System.Data.Entity.Infrastructure;
using TallyJ.Code.Data;

namespace TallyJ.CoreModels
{
  public enum DbAction
  {
    Add,
    Attach,
    /// <summary>
    /// Does not Save... will be removed?
    /// </summary>
    Save,
    AttachAndRemove
  }

  public abstract class ElectionAnalyzerCore : DataConnectedModel
  {
    private const int ThresholdForCloseVote = 3;
    protected readonly Savers Savers;

    //private readonly Func<int> _saveChanges;
    //private List<Ballot> _fakeBallots;
    private Election _election;
    //private List<Person> _fakePeople;
    //private List<ResultSummary> _fakeResultSummaries;
    //private List<ResultTie> _fakeResultTies;
    //private List<Result> _fakeResults;
    //private List<VoteInfo> _fakeVoteinfos;
    //private List<Vote> _fakeVotes;
    protected IStatusUpdateHub _hub;
    private List<Result> _results;
    private List<ResultSummary> _resultSummaries;
    private List<ResultTie> _resultTies;
    private List<VoteInfo> _voteInfos;
    private List<Ballot> _ballots;
    private List<Vote> _votes;
    private List<Person> _people;

    //private ResultSummaryCacher _localResultSummaryCacher;

    protected ElectionAnalyzerCore()
    {
      _election = UserSession.CurrentElection;
      _hub = new AnalyzeHub();
      Savers = new Savers(Db);
    }
    protected ElectionAnalyzerCore(Election election, IStatusUpdateHub hub = null)
    {
      _election = election;
      _hub = hub ?? new AnalyzeHub();
      Savers = new Savers(Db);
    }

    protected ElectionAnalyzerCore(IAnalyzerFakes fakes)
    {
      IsFaked = true;

      _election = UserSession.CurrentElection;
      _hub = fakes.FakeHub;
      Db = fakes.DbContext;
      Savers = new Savers(Db);
    }

    public IStatusUpdateHub AnalyzeHub { get { return _hub; } }

    public bool IsFaked { get; private set; }

    public ResultSummary ResultSummaryCalc { get; private set; }
    public ResultSummary ResultSummaryFinal { get; private set; }
    //public ResultSummary ResultSummaryManual { get; private set; }

    //protected Func<Result, Result> RemoveResult
    //{
    //  get { return _deleteResult ?? Db.Result.Remove; }
    //}

    ///// <Summary>Remove this result from the datastore</Summary>
    //protected Func<ResultTie, ResultTie> RemoveResultTie
    //{
    //  get { return _deleteResultTie ?? ResultTieSaver(DbAction.AttachAndRemove,  Db.ResultTie.Remove; }
    //}
    /// <Summary>Remove this result from the datastore</Summary>
    /// <Summary>Save all datastore changes</Summary>
    //protected Func<int> SaveChanges
    //{
    //  get { return _saveChanges ?? Db.SaveChanges; }
    //}

    //protected Func<IEnumerable<T>, int> BulkInsert(IEnumerable<T> objects)
    //{
    //  get {
    //    if (_saveChanges != null) {
    //      _saveChanges();
    //      return 0;
    //    }
    //    return Db.BulkInsert(; }
    //}

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
    //            new VoteCacher(Db).UpdateItemAndSaveCache(vote);
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
    //            new BallotCacher(Db).UpdateItemAndSaveCache(ballot);
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
    //            new ResultCacher(Db).UpdateItemAndSaveCache(result);
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
    //            new ResultCacher(Db).UpdateItemAndSaveCache(result);
    //          }
    //          break;
    //
    //        case DbAction.AttachAndRemove:
    //          if (!IsFaked)
    //          {
    //            new ResultCacher(Db).RemoveItemAndSaveCache(result);
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
    ////        new ResultSummaryCacher(Db).UpdateItemAndSaveCache(resultSummary);
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
    //            new ResultSummaryCacher(Db).UpdateItemAndSaveCache(resultSummary);
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
    //            new ResultSummaryCacher(Db).UpdateItemAndSaveCache(resultSummary);
    //          }
    //          break;
    //
    //        case DbAction.AttachAndRemove:
    //          if (!IsFaked)
    //          {
    //            new ResultSummaryCacher(Db).RemoveItemAndSaveCache(resultSummary);
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
    //            new ResultTieCacher(Db).UpdateItemAndSaveCache(resultTie);
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
    //            new ResultTieCacher(Db).UpdateItemAndSaveCache(resultTie);
    //          }
    //          break;
    //
    //        case DbAction.AttachAndRemove:
    //          if (!IsFaked && resultTie.C_RowId > 0)
    //          {
    //            new ResultTieCacher(Db).RemoveItemAndSaveCache(resultTie);
    //            Db.ResultTie.Attach(resultTie);
    //            Db.ResultTie.Remove(resultTie);
    //          }
    //          break;
    //
    //        default:
    //          throw new ArgumentOutOfRangeException("action");
    //      }
    //    }

    private void ClearInMemoryCachedInfo()
    {
      new BallotCacher().DropThisCache();
      //new ElectionCacher().DropThisCache();
      //new LocationCacher().DropThisCache();
      new PersonCacher().DropThisCache();
      new ResultCacher().DropThisCache();
      new ResultSummaryCacher().DropThisCache();
      new ResultTieCacher().DropThisCache();
      //new TellerCacher().DropThisCache();
      new VoteCacher().DropThisCache();
    }


    /// <Summary>Current Results records</Summary>
    public List<Person> People
    {
      get
      {
        return _people ?? (_people = new PersonCacher(Db).AllForThisElection);
      }
    }

    /// <Summary>Current Results records</Summary>
    public List<ResultTie> ResultTies
    {
      get
      {
        if (_resultTies != null) return _resultTies;
        return _resultTies = new ResultTieCacher(Db).AllForThisElection;
      }
    }

    internal Election TargetElection
    {
      get { return _election; }
    }

    /// <Summary>Votes are loaded, in case DB updates are required.</Summary>
    public List<Vote> Votes
    {
      get { return _votes ?? (_votes = new VoteCacher(Db).AllForThisElection); }
    }

    #region IElectionAnalyzer Members

    public List<Ballot> Ballots
    {
      get { return _ballots ?? (_ballots = new BallotCacher(Db).AllForThisElection); }
    }

    /// <Summary>Current Results records</Summary>
    public List<Result> Results
    {
      get { return _results ?? (_results = new ResultCacher(Db).AllForThisElection); }
    }

    public List<ResultSummary> ResultSummaries
    {
      get { return _resultSummaries ?? (_resultSummaries = new ResultSummaryCacher(Db).AllForThisElection); }
    }

    /// <Summary>Current VoteInfo records. They are detached, so no updates can be done</Summary>
    public List<VoteInfo> VoteInfos
    {
      get
      {
        return _voteInfos ?? (_voteInfos = Votes
                  .JoinMatchingOrNull(People, v => v.PersonGuid, p => p.PersonGuid, (v, p) => new { v, p })
                  .Select(
                    g =>
                      new VoteInfo(g.v, TargetElection, Ballots.Single(b => b.BallotGuid == g.v.BallotGuid),
                        UserSession.CurrentLocation, g.p))
                  .OrderBy(vi => vi.PositionOnBallot)
                  .ToList());
      }
    }

    /// <Summary>Check locally and in DB to see if the result is available at the moment</Summary>
    public bool IsResultAvailable
    {
      get
      {
        if (ResultSummaryFinal != null && ResultSummaryFinal.C_RowId > 0)
        {
          return true;
        }

        ResultSummaryFinal = ResultSummaries
          .FirstOrDefault(rs => rs.ResultType == ResultType.Final);

        return ResultSummaryFinal != null;
      }
    }

    //public ITallyJDbContext DbContext {
    //  get { return _dbContext ?? (_dbContext = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
    //}

    /// <Summary>In the Core, do some common results generation</Summary>
    public abstract void AnalyzeEverything();

    public void PrepareForAnalysis()
    {
      ClearInMemoryCachedInfo();

      _hub.StatusUpdate("Starting Analysis from computer " + UserSession.CurrentComputerCode);
      var electionGuid = TargetElection.ElectionGuid;
      //if (!IsFaked)
      //{
      var resultSummaryCacher = new ResultSummaryCacher(Db);
      var summaries = resultSummaryCacher.AllForThisElection;
      resultSummaryCacher.RemoveItemsAndSaveCache(summaries.Where(rs => rs.ResultType != ResultType.Manual));

      Db.ResultSummary.RemoveRange(
        Db.ResultSummary.Where(r => r.ElectionGuid == electionGuid && r.ResultType != ResultType.Manual)
        );

      RefreshBallotStatuses();

      // attach results, but don't save yet
      Results.ForEach(delegate (Result result)
      {
        Savers.ResultSaver(DbAction.Attach, result);
        InitializeSomeProperties(result);
      });

      PrepareResultSummaries();

      FillResultSummaryCalc();
    }

    public void RefreshBallotStatuses()
    {
      // first refresh person vote statuses
      new PeopleModel().EnsureFlagsAreRight(People, _hub, Savers.PersonSaver);

      // then refresh all votes
      _hub.StatusUpdate("Reviewing votes");
      VoteAnalyzer.UpdateAllStatuses(VoteInfos, Votes, Savers.VoteSaver);

      // then refresh all ballots
      _hub.StatusUpdate("Reviewing ballots");
      var ballotAnalyzer = new BallotAnalyzer(TargetElection, Savers.BallotSaver);
      ballotAnalyzer.UpdateAllBallotStatuses(Ballots, VoteInfos);
    }

    /// <summary>
    ///   Load the Calc and Final summaries
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
          ResultSummaries.Add(ResultSummaryCalc);
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
          ResultSummaries.Add(ResultSummaryFinal);
        }
      }
    }

    #endregion

    public void FinalizeSummaries()
    {
      _hub.StatusUpdate("Finalizing");

      CombineCalcAndManualSummaries();

      ResultSummaryFinal.UseOnReports =
        ResultSummaryFinal.BallotsNeedingReview == 0
          && ResultTies.All(rt => rt.IsResolved.AsBoolean())
          && ResultSummaryFinal.NumBallotsWithManual == ResultSummaryFinal.SumOfEnvelopesCollected;

    }

    protected void FinalizeResultsAndTies()
    {
      _hub.StatusUpdate("Checking for ties");

      // remove any results no longer needed
      Results.Where(r => r.VoteCount.AsInt() == 0)
        .ToList()
        .ForEach(r =>
        {
          Savers.ResultSaver(DbAction.AttachAndRemove, r);
                //Results.Remove(r);
              });

      // remove any existing Tie info
      Db.ResultTie.RemoveRange(Db.ResultTie.Where(rt => rt.ElectionGuid == TargetElection.ElectionGuid));
      ResultTies.Clear();

      DetermineOrderAndSections();
      AnalyzeForTies();
    }

    /// <Summary>
    ///   Assign an ordinal rank number to all results. Ties are NOT reflected in rank number. If there is a tie, they
    ///   are sorted "randomly".
    /// </Summary>
    internal void DetermineOrderAndSections()
    {
      var election = TargetElection;

      var ordinalRank = 0;
      var ordinalRankInExtra = 0;

      // use RowId after VoteCount to ensure results are consistent when there is a tie in the VoteCount
      var people = People;
      foreach (
        var result in
          Results.OrderByDescending(r => r.VoteCount)
            .ThenByDescending(r => r.TieBreakCount.GetValueOrDefault())
            .ThenBy(r =>
            {
              var person = people.FirstOrDefault(p => p.PersonGuid == r.PersonGuid);
              if (person != null)
              {
                return person.FullNameFL;
              }
              return r.C_RowId.ToString();
            }))
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
      var foundFirstOneInOther = false;

      // round 1
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

            if (!foundFirstOneInOther && result.Section == ResultHelper.Section.Other)
            {
              foundFirstOneInOther = true;
            }

            if (aboveResult.TieBreakGroup.HasNoContent())
            {
              aboveResult.TieBreakGroup = nextTieBreakGroup;
              nextTieBreakGroup++;
            }
            result.TieBreakGroup = aboveResult.TieBreakGroup;
          }
          else
          {
            // not tied with one above
            if (foundFirstOneInOther)
            {
              // already finished a tie break in Other
              // don't bother marking others
              break;
            }
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

      // last one?
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
        ResultTies.Add(resultTie);

        AnalyzeTieGroup(resultTie, Results.Where(r => r.TieBreakGroup == code).OrderBy(r => r.Rank).ToList());
      }

      // set global value
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
      var isResolved = true;

      results.ForEach(delegate (Result r)
      {
        r.TieBreakRequired = !(groupOnlyInOther || groupOnlyInTop);

              // expressed in the positive for developers!
              var stillTied = results.Any(other => other != r
                                          && other.TieBreakCount.AsInt() == r.TieBreakCount.AsInt()
                                          && (other.Section != r.Section || r.Section == ResultHelper.Section.Extra)
                                          );

        if (stillTied)
        {
          isResolved = false;
        }
      });

      // apply to each result
      results.ForEach(r => r.IsTieResolved = isResolved);
      resultTie.IsResolved = isResolved;

      // if others are involved, set them to show
      if (groupInOther && (groupInTop || groupInExtra))
      {
        results.Where(r => r.Section == ResultHelper.Section.Other)
          .ToList()
          .ForEach(r => r.ForceShowInOther = true);
      }

      if (groupInTop)
      {
        if (groupOnlyInTop)
        {
          // default... tie-break not needed
        }
        else
        {
          resultTie.NumToElect += results.Count(r => r.Section == ResultHelper.Section.Top);
          resultTie.TieBreakRequired = true;
        }
      }

      var extrasToBeDistinct = 0;
      if (groupInExtra)
      {
        resultTie.TieBreakRequired = true;
        extrasToBeDistinct = results.Count(r => r.Section == ResultHelper.Section.Extra) - 1;

        if (!groupInTop)
        {
          resultTie.NumToElect += results.Count(r => r.Section == ResultHelper.Section.Extra);
        }
      }

      //var numResolved = 0;
      //if (resultTie.NumToElect > 0)
      //{
      //  //results are in descending order already, so starting at 0 is starting at the "top"
      //  for (int i = 0, max = results.Count; i < max; i++)
      //  {
      //    var result = results[i];
      //    if (!result.IsTieResolved.AsBoolean()) break;
      //    numResolved += 1;
      //  }
      //}

      //if (numResolved < resultTie.NumToElect + extrasToBeDistinct)
      //{
      //  resultTie.IsResolved = false;
      //}
      //else
      //{
      //  resultTie.IsResolved = true;
      //}

      if (resultTie.NumInTie == resultTie.NumToElect)
      {
        // need to vote for one less than the number to be elected
        resultTie.NumToElect--;
      }

      if (resultTie.TieBreakRequired.Value)
      {
        // required? ensure each has a number
        results.ForEach(r =>
        {
          if (r.TieBreakCount == null)
          {
            r.TieBreakCount = 0;
          }
        });
      }
      else
      {
        // not required? remove any counts
        results.ForEach(r => r.TieBreakCount = null);
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

      result.Section = "";

      // result.TieBreakCount = null;  -- don't clear this, as it may be entered after tie-break vote is held

      result.TieBreakGroup = null;
      result.TieBreakRequired = false;

      result.VoteCount = 0;
    }

    protected void FillResultSummaryCalc()
    {
      ResultSummaryCalc.NumVoters = People.Count(p => p.VotingMethod.HasContent());
      ResultSummaryCalc.NumEligibleToVote = People.Count(p => p.CanVote.AsBoolean());

      ResultSummaryCalc.InPersonBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.InPerson);
      ResultSummaryCalc.MailedInBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.MailedIn);
      ResultSummaryCalc.DroppedOffBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.DroppedOff);
      ResultSummaryCalc.CalledInBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.CalledIn);
      ResultSummaryCalc.OnlineBallots = People.Count(p => p.VotingMethod == VotingMethodEnum.Online);
      // ignore Registered
    }

    /// <summary>
    ///   Combine the automatic count with any values saved into a "Manual" result summary record
    /// </summary>
    public void CombineCalcAndManualSummaries()
    {
      var manualOverride = ResultSummaries.FirstOrDefault(rs => rs.ResultType == ResultType.Manual)
                        ?? new ResultSummary();

      // allow override of some

      ResultSummaryFinal.NumEligibleToVote = manualOverride.NumEligibleToVote.HasValue
        ? manualOverride.NumEligibleToVote.Value
        : ResultSummaryCalc.NumEligibleToVote.GetValueOrDefault();

      ResultSummaryFinal.InPersonBallots = manualOverride.InPersonBallots.HasValue
        ? manualOverride.InPersonBallots.Value
        : ResultSummaryCalc.InPersonBallots.GetValueOrDefault();

      ResultSummaryFinal.DroppedOffBallots = manualOverride.DroppedOffBallots.HasValue
        ? manualOverride.DroppedOffBallots.Value
        : ResultSummaryCalc.DroppedOffBallots.GetValueOrDefault();

      ResultSummaryFinal.MailedInBallots = manualOverride.MailedInBallots.HasValue
        ? manualOverride.MailedInBallots.Value
        : ResultSummaryCalc.MailedInBallots.GetValueOrDefault();

      ResultSummaryFinal.CalledInBallots = manualOverride.CalledInBallots.HasValue
        ? manualOverride.CalledInBallots.Value
        : ResultSummaryCalc.CalledInBallots.GetValueOrDefault();

      ResultSummaryFinal.OnlineBallots =
        ResultSummaryCalc.OnlineBallots.GetValueOrDefault();


      // no overrides

      // Received --> now is used for Valid ballots
      ResultSummaryFinal.BallotsReceived = ResultSummaryCalc.BallotsReceived.GetValueOrDefault();

      ResultSummaryFinal.NumVoters = manualOverride.NumVoters.HasValue
        ? manualOverride.NumVoters.Value
        : ResultSummaryCalc.NumVoters.GetValueOrDefault();

      ResultSummaryFinal.SpoiledManualBallots = manualOverride.SpoiledManualBallots;

      ResultSummaryFinal.BallotsNeedingReview = ResultSummaryCalc.BallotsNeedingReview;

      // add manual to calculated
      ResultSummaryFinal.SpoiledBallots =
           manualOverride.SpoiledManualBallots.GetValueOrDefault()
         + ResultSummaryCalc.SpoiledBallots.GetValueOrDefault();

      ResultSummaryFinal.SpoiledVotes = ResultSummaryCalc.SpoiledVotes;

      //ResultSummaryFinal.TotalVotes = manualInput.TotalVotes.HasValue
      //                                    ? manualInput.TotalVotes.Value
      //                                    : ResultSummaryCalc.TotalVotes.GetValueOrDefault();
    }
  }
}