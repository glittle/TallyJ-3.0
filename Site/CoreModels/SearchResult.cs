using System;

namespace TallyJ.CoreModels
{
  public class SearchResult
  {
    public Nullable<int> PersonId { get; set; }
    public Guid PersonGuid { get; set; }
    public string FullName { get; set; }
    public Nullable<System.Guid> Ineligible { get; set; }
    public Nullable<int> MatchType { get; set; }
    public Nullable<bool> CanReceiveVotes { get; set; }
    public int BestMatch { get; set; }
  }
}