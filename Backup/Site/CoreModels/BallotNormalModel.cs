using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.Models;

namespace TallyJ.CoreModels
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

    public override object BallotInfoForJs(vBallotInfo b)
    {
      return new
      {
        Id = b.C_RowId,
        Code = b.C_BallotCode,
        b.StatusCode,
        StatusCodeText = BallotStatusEnum.TextFor(b.StatusCode),
        b.SpoiledCount
      };
    }
  }
}