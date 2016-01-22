using System.Collections.Generic;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public interface IElectionAnalyzer
  {
    /// <Summary>Current Results records</Summary>
    List<Result> Results { get; }

    List<ResultSummary> ResultSummaries { get; }

    ResultSummaryCacher LocalResultSummaryCacher { get; }

    /// <Summary>Current Results records</Summary>
    ResultSummary ResultSummaryFinal { get; }

    /// <Summary>Current VoteInfo records</Summary>
    List<VoteInfo> VoteInfos { get; }

    /// <Summary>Indicate if the results are available, or need to be generated</Summary>
    bool IsResultAvailable { get; }

    List<Ballot> Ballots { get; }

    ResultSummary AnalyzeEverything();

    void PrepareResultSummaries();

    void FinalizeSummaries();

    IStatusUpdateHub AnalyzeHub { get; }

  }
}