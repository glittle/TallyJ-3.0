using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class BallotHelper : DataConnectedModel
  {
    public int BallotCount(Guid locationGuid, bool isSingleName, List<Ballot> ballots = null, List<Vote> votes = null)
    {
      int sum;
      ballots = ballots ?? new BallotCacher(Db).AllForThisElection;

      if (isSingleName)
      {
        var allBallotGuids = ballots
          .Where(b => b.LocationGuid == locationGuid)
          .Select(b => b.BallotGuid).ToList();

        votes = votes ?? new VoteCacher(Db).AllForThisElection;

        sum = votes.Where(v => allBallotGuids.Contains(v.BallotGuid))
          .Sum(vi => vi.SingleNameElectionCount).AsInt();
      }
      else
      {
        sum = ballots.Count(b => b.LocationGuid == locationGuid);
      }
      return sum;
    }


    public int BallotCount(Guid locationGuid, string computerCode, bool isSingleName, List<Ballot> ballots = null, List<Vote> votes = null)
    {
      int sum;
      ballots = ballots ?? new BallotCacher(Db).AllForThisElection;

      if (isSingleName)
      {
        var allBallotGuids = ballots.Where(b => b.LocationGuid == locationGuid && b.ComputerCode == computerCode)
          .Select(b => b.BallotGuid).ToList();

        votes = votes ?? new VoteCacher(Db).AllForThisElection;
        sum = votes.Where(v => allBallotGuids.Contains(v.BallotGuid))
          .Sum(vi => vi.SingleNameElectionCount).AsInt();
      }
      else
      {
        sum = ballots.Count(b => b.LocationGuid == locationGuid && b.ComputerCode == computerCode);
      }
      return sum;
    }


    public List<VoteInfo> VoteInfosForBallot(Ballot ballot, List<Vote> allVotes)
    {
      return (allVotes ?? new VoteCacher(Db).AllForThisElection.ToList())
                 .Where(v => v.BallotGuid == ballot.BallotGuid)
                 .JoinMatchingOrNull(new PersonCacher(Db).AllForThisElection, v => v.PersonGuid, p => p.PersonGuid, (v, p) => new { v, p })
                 .Select(g => new VoteInfo(g.v, UserSession.CurrentElection, ballot, UserSession.CurrentLocation, g.p))
                 .OrderBy(vi => vi.PositionOnBallot)
                 .ToList();
    }

  }
}
