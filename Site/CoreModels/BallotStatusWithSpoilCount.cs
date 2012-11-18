using TallyJ.Code.Enumerations;

namespace TallyJ.CoreModels
{
  public class BallotStatusWithSpoilCount
  {
    public BallotStatusEnum Status { get; set; }
    public int SpoiledCount { get; set; }
    public int NumSingleNameVotes { get; set; }
  }
}