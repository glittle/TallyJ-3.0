namespace TallyJ.Models
{
  public class BallotSingleModel : BallotModelCore
  {
    public override int NextBallotNumAtComputer()
    {
      // for single name ballots, always use 0
      return 0;
    }
  }
}