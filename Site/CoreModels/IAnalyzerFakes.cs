using System.Collections.Generic;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public interface IAnalyzerFakes
  {
    ITallyJDbContext DbContext { get; }

    //ResultSummary ResultSummaryManual { get; set; }
    //List<ResultTie> ResultTies { get; set; }
    //List<Result> Results { get; set; }
    //List<ResultSummary> ResultSummaries { get; set; }
    IStatusUpdateHub FakeHub { get; }
    //Result RemoveResult(Result input);
    //Result AddResult(Result arg);
    //ResultSummary AddResultSummary(ResultSummary arg);
    //int SaveChanges();
    //ResultTie RemoveResultTie(ResultTie arg);
    //ResultTie AddResultTie(ResultTie arg);
  }
}