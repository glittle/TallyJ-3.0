using TallyJ.EF;

namespace TallyJ.Models
{
  public class BallotSingleModel : BallotModelCore
  {
    public override int NextBallotNumAtComputer()
    {
      // for single name ballots, always use 1
      return 1;
    }

    protected override object BallotForJson(vBallotInfo b)
    {
      return new
               {
                 Id = b.C_RowId,
                 Code = b.C_BallotCode,
                 Location = b.LocationName,
                 LocationSort = b.LocationSortOrder,
                 b.LocationId,
                 b.TallyStatus
               };
    }
  }
}