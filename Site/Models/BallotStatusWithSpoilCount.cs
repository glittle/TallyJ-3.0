using TallyJ.Code.Enumerations;

namespace TallyJ.Models
{
  public class BallotStatusWithSpoilCount
  {
    public BallotStatusEnum Status { get; set; }
    public int SpoiledCount { get; set; }
  }
}