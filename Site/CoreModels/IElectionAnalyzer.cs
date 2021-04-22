using System.Collections.Generic;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public interface IElectionAnalyzer
  {
    /// <Summary>Current Results records</Summary>
    List<Result> Results { get; }

    List<ResultTie> ResultTies { get; }

    List<ResultSummary> ResultSummaries { get; }


    /// <Summary>Current Results records</Summary>
    ResultSummary ResultSummaryFinal { get; }

    /// <Summary>Current VoteInfo records</Summary>
    List<VoteInfo> VoteInfos { get; }

    /// <Summary>Indicate if the results are available, or need to be generated</Summary>
    bool IsResultAvailable { get; }

    List<Ballot> Ballots { get; }

    void AnalyzeEverything();

    void PrepareResultSummaries();

    void RefreshBallotStatuses();

    void FinalizeSummaries();

    IStatusUpdateHub AnalyzeHub { get; }

  }
}