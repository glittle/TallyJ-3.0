using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class BallotNormalModel : BallotModelCore
  {
    public override int NextBallotNumAtComputer()
    {
      var computerCode = UserSession.CurrentComputerCode;

      var nextBallotNum = 1 + Db.vBallotInfoes.Where(b => b.ElectionGuid == UserSession.CurrentElectionGuid
                                                     && b.ComputerCode == computerCode)
                                .OrderByDescending(b => b.BallotNumAtComputer)
                                .Take(1)
                                .Select(b => b.BallotNumAtComputer)
                                .SingleOrDefault();

      return nextBallotNum;
    }

    protected override object BallotForJson(vBallotInfo b)
    {
      return new
      {
        Id = b.C_RowId,
        Code = b.C_BallotCode,
        //Location = b.LocationName,
        //LocationSort = b.LocationSortOrder,
        //b.LocationId,
        StatusCode = BallotStatusEnum.TextFor(b.StatusCode),
        //b.TallyStatus
      };
    }
  }
}