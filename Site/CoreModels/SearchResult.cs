using System;

namespace TallyJ.CoreModels
{
  public class SearchResult
  {
    public Nullable<int> Id { get; set; }
    public Guid PersonGuid { get; set; }
    public string Name { get; set; }
    public Nullable<System.Guid> Ineligible { get; set; }
    public Nullable<int> MatchType { get; set; }
    public bool CanReceiveVotes { get; set; }
    public bool CanVote { get; set; }
    public int BestMatch { get; set; }
    public long RowVersion { get; set; }
    public string Extra { get; set; }
  }
}