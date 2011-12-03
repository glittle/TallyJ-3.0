using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.Models
{
  public class BallotNormalModel : BallotModelCore
  {
    public override int NextBallotNumAtComputer()
    {
      var computerCode = UserSession.CurrentComputerCode;

      var nextBallotNum = 1 + Db.vBallots.Where(b => b.ElectionGuid == UserSession.CurrentElectionGuid
                                                     && b.ComputerCode == computerCode)
                                .OrderByDescending(b => b.BallotNumAtComputer)
                                .Take(1)
                                .Select(b => b.BallotNumAtComputer)
                                .SingleOrDefault();

      return nextBallotNum;
    }
  }
}