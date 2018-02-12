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
      var ballotCounts = new VoteCacher().AllForThisElection
        .Where(v => v.BallotGuid == b.BallotGuid)
        .Sum(v => v.SingleNameElectionCount);
      return new
      {
        Id = b.C_RowId,
        Code = b.C_BallotCode,
        b.ComputerCode,
        Count = ballotCounts
      };
    }
  }
}