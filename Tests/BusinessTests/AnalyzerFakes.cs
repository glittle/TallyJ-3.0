using System;
using System.Collections.Generic;

using TallyJ.CoreModels.Hubs;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.Code;
using TallyJ.EF;
using System.Dynamic;

namespace Tests.BusinessTests
{

  public class AnalyzerFakes : IAnalyzerFakes
  {
    public AnalyzerFakes()
    {
      Results = new List<Result>();
      ResultTies = new List<ResultTie>();
      ResultSummaries = new List<ResultSummary>();
      FakeHub = new FakeHub();
    }

    private int _rowCounter;
    private ResultSummary _resultSummaryManual;

    public ResultSummary ResultSummaryManual
    {
      get
      {
        return _resultSummaryManual ?? (_resultSummaryManual = new ResultSummary { ResultType = ResultType.Manual });
      }
      set { _resultSummaryManual = value; }
    }

    public List<ResultTie> ResultTies { get; set; }
    public List<Result> Results { get; set; }
    public List<ResultSummary> ResultSummaries { get; set; }

    public IAnalyzeHub FakeHub
    {
      get; set;
    }

    public Result RemoveResult(Result input)
    {
      throw new ApplicationException("Should not be called in tests!");
    }

    public Result AddResult(Result arg)
    {
      arg.C_RowId = ++_rowCounter;
      Results.Add(arg);
      return arg;
    }

    public ResultSummary AddResultSummary(ResultSummary arg)
    {
      arg.C_RowId = ++_rowCounter;
      ResultSummaries.Add(arg);
      return arg;
    }

    public int SaveChanges()
    {
      return 0;
    }

    public ResultTie RemoveResultTie(ResultTie arg)
    {
      throw new ApplicationException("Should not be called in tests!");
    }

    public ResultTie AddResultTie(ResultTie arg)
    {
      arg.C_RowId = ++_rowCounter;
      ResultTies.Add(arg);
      return arg;
    }
  }

  public class FakeHub : IAnalyzeHub
  {
    public void LoadStatus(string msg, bool msgIsTemp = false)
    {
      Console.WriteLine(msg);
    }
  }
}