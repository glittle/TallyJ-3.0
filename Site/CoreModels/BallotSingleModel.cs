using TallyJ.Code.Enumerations;
using TallyJ.Models;

namespace TallyJ.CoreModels
{
  public class BallotSingleModel : BallotModelCore
  {
    public override int NextBallotNumAtComputer()
    {
      // for single name ballots, always use 1
      return 1;
    }

    public override object BallotInfoForJs(vBallotInfo b)
    {
      return new
               {
                 Id = b.C_RowId,
                 Code = b.C_BallotCode,
                 Status = BallotStatusEnum.TextFor(b.StatusCode),
                 Location = b.LocationName,
                 LocationSort = b.LocationSortOrder,
                 b.LocationId,
                 TallyStatus = ElectionTallyStatusEnum.TextFor(b.TallyStatus)
               };
    }
  }
}