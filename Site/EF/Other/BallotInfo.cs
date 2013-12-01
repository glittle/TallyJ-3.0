using System;

namespace TallyJ.EF
{
  public class BallotInfo
  {
    public BallotInfo(Ballot b, VoteInfo vi)
    {

    }

    public Guid LocationGuid { get; set; }
    public Guid BallotGuid { get; set; }
    public string StatusCode { get; set; }
    public string ComputerCode { get; set; }
    public int C_RowId { get; set; }
    public int BallotNumAtComputer { get; set; }
    public string C_BallotCode { get; set; }
    public Guid? TellerAtKeyboard { get; set; }
    public Guid? TellerAssisting { get; set; }
    public byte[] C_RowVersion { get; set; }
    public Guid ElectionGuid { get; set; }
    public int LocationId { get; set; }
    public string LocationName { get; set; }
    public int? LocationSortOrder { get; set; }
    public string TallyStatus { get; set; }
    public string TellerAtKeyboardName { get; set; }
    public string TellerAssistingName { get; set; }
    public long? RowVersionInt { get; set; }
    public int? SpoiledCount { get; set; }
    public int? VotesChanged { get; set; }
  }
}