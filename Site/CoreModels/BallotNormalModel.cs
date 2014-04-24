using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class BallotNormalModel : BallotModelCore
  {
    public override int NextBallotNumAtComputer()
    {
      var computerCode = UserSession.CurrentComputerCode;

      var nextBallotNum = 1 + new BallotCacher().AllForThisElection.Where(b => b.ComputerCode == computerCode)
                                .OrderByDescending(b => b.BallotNumAtComputer)
                                .Take(1)
                                .Select(b => b.BallotNumAtComputer)
                                .SingleOrDefault();

      return nextBallotNum;
    }

    public override object BallotInfoForJs(Ballot b)
    {
      var votes = VoteInfosForBallot(b, new VoteCacher().AllForThisElection.ToList());
      var spoiledCount = votes.Count(v => v.VoteStatusCode != VoteHelper.VoteStatusCode.Ok);
      return new
      {
        Id = b.C_RowId,
        Code = b.C_BallotCode,
        b.StatusCode,
        StatusCodeText = BallotStatusEnum.TextFor(b.StatusCode),
        SpoiledCount = spoiledCount
      };
    }
  }
}