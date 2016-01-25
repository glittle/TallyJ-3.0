using System;
using System.Linq;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class Savers : DataConnectedModel
  {
    //private readonly bool _isInTest;
    int _tempRowId = -1;

    //private readonly Func<Result, Result> _addResult;
    //private readonly Func<ResultSummary, ResultSummary> _addResultSummary;
    //private readonly Func<ResultTie, ResultTie> _addResultTie;
    //private ITallyJDbContext dbContext;

    //public Savers()
    //{
    //  _isInTest = false;
    //}

    //public Savers(bool isInTest)
    //{
    //  _isInTest = isInTest;
    //}

    //public Savers(IAnalyzerFakes fakes)
    //{
    //  _addResult = fakes.AddResult;
    //  _addResultSummary = fakes.AddResultSummary;
    //  _addResultTie = fakes.AddResultTie;
    //  _isInTest = true;
    //}

    public Savers(ITallyJDbContext dbContext)
    {
      Db = dbContext;
    }

    public void PersonSaver(DbAction action, Person person)
    {
      switch (action)
      {
        case DbAction.Attach:
          //if (!_isInTest)
          {
            if (Db.Person.Local.All(l => l.C_RowId != person.C_RowId))
            {
              Db.Person.Attach(person);
            }
          }
          break;

        case DbAction.Save:
          //if (!_isInTest)
          {
            //new PersonCacher(Db).UpdateItemAndSaveCache(person);
          }
          break;

        default:
          throw new ArgumentOutOfRangeException("action");
      }
    }

    public void VoteSaver(DbAction action, Vote vote)
    {
      switch (action)
      {
        case DbAction.Attach:
          //if (!_isInTest)
          {
            if (Db.Vote.Local.All(l => l.C_RowId != vote.C_RowId))
            {
              Db.Vote.Attach(vote);
            }
          }
          break;

        case DbAction.Save:
          //if (!_isInTest)
          {
            //new VoteCacher(Db).UpdateItemAndSaveCache(vote);
          }
          break;

        default:
          throw new ArgumentOutOfRangeException("action");
      }
    }

    public void BallotSaver(DbAction action, Ballot ballot)
    {
      switch (action)
      {
        case DbAction.Attach:
          //if (!_isInTest)
          {
            if (Db.Ballot.Local.All(l => l.C_RowId != ballot.C_RowId))
            {
              Db.Ballot.Attach(ballot);
            }
          }
          break;

        case DbAction.Save:
          //if (!_isInTest)
          {
            //new BallotCacher(Db).UpdateItemAndSaveCache(ballot);
          }
          break;

        default:
          throw new ArgumentOutOfRangeException("action");
      }
    }

    public void ResultSaver(DbAction action, Result result)
    {
      switch (action)
      {
        case DbAction.Add:
          //if (!_isInTest)
          {
            result.C_RowId = _tempRowId--;
            Db.Result.Add(result);
            //new ResultCacher(Db).UpdateItemAndSaveCache(result);
          }
          //else
          //{
          //  _addResult(result);
          //}
          break;

        case DbAction.Attach:
          //if (!_isInTest)
          {
            if (Db.Result.Local.All(r => r.C_RowId != result.C_RowId))
            {
              Db.Result.Attach(result);
            }
          }
          break;

        case DbAction.Save:
          //if (!_isInTest)
          {
            //new ResultCacher(Db).UpdateItemAndSaveCache(result);
          }
          break;

        case DbAction.AttachAndRemove:
          //if (!_isInTest)
          {
            //new ResultCacher(Db).RemoveItemAndSaveCache(result);
            if (Db.Result.Local.All(r => r.C_RowId != result.C_RowId))
            {
              Db.Result.Attach(result);
            }
            Db.Result.Remove(result);
          }
          break;

        default:
          throw new ArgumentOutOfRangeException("action");
      }
    }

    /// <Summary>Add this result to the datastore</Summary>
    //    protected void AddResultSummary(ResultSummary resultSummary)
    //    {
    //      ResultSummaries.Add(resultSummary);
    //      if (_addResultSummary != null)
    //      {
    //        _addResultSummary(resultSummary);
    //      }
    //      else
    //      {
    //        resultSummary.C_RowId = tempRowId--;
    //        Db.ResultSummary.Add(resultSummary);
    //        new ResultSummaryCacher(Db).UpdateItemAndSaveCache(resultSummary);
    //      }
    //    }
    public void ResultSummarySaver(DbAction action, ResultSummary resultSummary)
    {
      switch (action)
      {
        case DbAction.Add:
          resultSummary.C_RowId = _tempRowId--;
          //if (!_isInTest)
          {
            Db.ResultSummary.Add(resultSummary);
            //new ResultSummaryCacher(Db).UpdateItemAndSaveCache(resultSummary);
          }
          //else
          //{
          //  _addResultSummary(resultSummary);
          //}
          break;

        case DbAction.Attach:
          //if (!_isInTest)
          {
            if (Db.ResultSummary.Local.All(r => r.C_RowId != resultSummary.C_RowId))
            {
              Db.ResultSummary.Attach(resultSummary);
            }
          }
          break;

        case DbAction.Save:
          //if (!_isInTest)
          {
            //new ResultSummaryCacher(Db).UpdateItemAndSaveCache(resultSummary);
          }
          break;

        case DbAction.AttachAndRemove:
          //if (!_isInTest)
          {
            //new ResultSummaryCacher(Db).RemoveItemAndSaveCache(resultSummary);
            if (Db.ResultSummary.Local.All(r => r.C_RowId != resultSummary.C_RowId))
            {
              Db.ResultSummary.Attach(resultSummary);
            }
            Db.ResultSummary.Remove(resultSummary);
          }
          break;

        default:
          throw new ArgumentOutOfRangeException("action");
      }
    }

    /// <Summary>Add this resultTie to the datastore</Summary>
    //protected Func<ResultTie, ResultTie> AddResultTie
    //{
    //  get { return _addResultTie ?? Db.ResultTie.Add; }
    //}
    public void ResultTieSaver(DbAction action, ResultTie resultTie)
    {
      switch (action)
      {
        case DbAction.Add:
          //if (!_isInTest)
          {
            resultTie.C_RowId = _tempRowId--;
            Db.ResultTie.Add(resultTie);
            //new ResultTieCacher(Db).UpdateItemAndSaveCache(resultTie);
          }
          //else
          //{
          //  _addResultTie(resultTie);
          //}
          break;

        case DbAction.Attach:
          //if (!_isInTest)
          {
            if (Db.ResultTie.Local.All(r => r.C_RowId != resultTie.C_RowId))
            {
              Db.ResultTie.Attach(resultTie);
            }
          }
          break;

        case DbAction.Save:
          //if (!_isInTest)
          {
            //new ResultTieCacher(Db).UpdateItemAndSaveCache(resultTie);
          }
          break;

        case DbAction.AttachAndRemove:
          //if (!_isInTest && resultTie.C_RowId > 0)
          if (resultTie.C_RowId > 0)
          {
            //new ResultTieCacher(Db).RemoveItemAndSaveCache(resultTie);
            if (Db.ResultTie.Local.All(r => r.C_RowId != resultTie.C_RowId))
            {
              Db.ResultTie.Attach(resultTie);
            }
            Db.ResultTie.Remove(resultTie);
          }
          break;

        default:
          throw new ArgumentOutOfRangeException("action");
      }
    }
  }
}