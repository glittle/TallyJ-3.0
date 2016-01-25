using System.Collections.Generic;
using System.Linq;
using TallyJ.Code.Enumerations;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class BallotSingleModel : BallotModelCore
  {
    public override int NextBallotNumAtComputer()
    {
      // for single name ballots, always use 1
      return 1;
    }

    public override object BallotInfoForJs(Ballot b, List<Vote> allVotes)
    {
      var loc = new LocationCacher(Db).AllForThisElection.Single(l => l.LocationGuid == b.LocationGuid);
      return new
               {
                 Id = b.C_RowId,
                 Code = b.C_BallotCode,
                 Status = BallotStatusEnum.TextFor(b.StatusCode),
                 Location = loc.Name,
                 LocationSort = loc.SortOrder,
                 LocationId = loc.C_RowId,
                 TallyStatus = ElectionTallyStatusEnum.TextFor(loc.TallyStatus)
               };
    }
  }
}