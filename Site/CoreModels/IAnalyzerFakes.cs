using TallyJ.EF;

namespace TallyJ.CoreModels
{
    public interface IAnalyzerFakes
    {
        ResultSummary ResultSummaryManual { get; set; }
        Result RemoveResult(Result input);
        Result AddResult(Result arg);
        ResultSummary AddResultSummary(ResultSummary arg);
        int SaveChanges();
        ResultTie RemoveResultTie(ResultTie arg);
        ResultTie AddResultTie(ResultTie arg);
    }
}