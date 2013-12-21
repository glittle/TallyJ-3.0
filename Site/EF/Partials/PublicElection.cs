using System;

namespace TallyJ.EF
{
  public class PublicElection
  {
    public PublicElection(Election election)
    {
      C_RowId = election.C_RowId;
      Name = election.Name;
      ElectionPasscode = election.ElectionPasscode;
      ElectionGuid = election.ElectionGuid;
    }

    public int C_RowId { get; set; }
    public string Name { get; set; }
    public string ElectionPasscode { get; set; }
    public Guid ElectionGuid { get; set; }
  }
}