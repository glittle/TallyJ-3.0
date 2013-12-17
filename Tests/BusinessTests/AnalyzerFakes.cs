using System;
using System.Collections.Generic;

using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.Code;
using TallyJ.EF;

namespace Tests.BusinessTests
{

    public class AnalyzerFakes : IAnalyzerFakes
    {
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

        public Result RemoveResult(Result input)
        {
            throw new ApplicationException("Should not be called in tests!");
        }

        public Result AddResult(Result arg)
        {
            arg.C_RowId = ++_rowCounter;
            return arg;
        }

        public ResultSummary AddResultSummary(ResultSummary arg)
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