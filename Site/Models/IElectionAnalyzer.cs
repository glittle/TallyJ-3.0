using System.Collections.Generic;
using TallyJ.EF;

namespace TallyJ.Models
{
  public interface IElectionAnalyzer
  {
    ResultSummary GenerateResults();

    /// <Summary>Current Results records</Summary>
    List<Result> Results { get; }

    /// <Summary>Current Results records</Summary>
    ResultSummary ResultSummaryAuto { get; }

    /// <Summary>Current VoteInfo records</Summary>
    List<vVoteInfo> VoteInfos { get; }

    /// <Summary>Indicate if the results are available, or need to be generated</Summary>
    bool IsResultAvailable { get; }

    List<Ballot> Ballots { get; }
  }
}