namespace TallyJ.CoreModels
{
  public class ElectionRules
  {
    public bool CanVoteLocked { get; set; }
    public bool CanReceiveLocked { get; set; }
    public bool NumLocked { get; set; }
    public bool ExtraLocked { get; set; }

    /// <summary>
    ///     Can Vote/Receive - All or Named people
    /// </summary>
    public string CanVote { get; set; }

    public string CanReceive { get; set; }

    public bool IsSingleNameElection { get; set; }

    public int Num { get; set; }
    public int Extra { get; set; }
  };
}