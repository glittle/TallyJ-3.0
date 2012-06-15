using System;
using System.Collections.Generic;
using TallyJ.EF;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.Code;

namespace Tests.BusinessTests
{
  
  public class AnalyzerFakes : IAnalyzerFakes
  {
    private int _rowCounter;

    public ResultSummary ResultSummary
    {
      get { return new ResultSummary {ResultType = ResultType.Automatic}; }
    }

    public Result RemoveResult(Result input)
    {
      throw new ApplicationException("Should not be called in tests!");
    }

    public Result AddResult(Result arg)
    {
      arg.C_RowId = ++_rowCounter;
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
      return arg;
    }
  }
}