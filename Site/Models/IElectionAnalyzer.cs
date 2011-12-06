using System.Collections.Generic;
using TallyJ.EF;

namespace TallyJ.Models
{
  public interface IElectionAnalyzer
  {
    void GenerateResults();

    /// <Summary>Current Results records</Summary>
    List<Result> Results { get; }

    /// <Summary>Current Results records</Summary>
    ResultSummary ResultSummaryAuto { get; }

    /// <Summary>Current VoteInfo records</Summary>
    List<vVoteInfo> VoteInfos { get; }
  }
}