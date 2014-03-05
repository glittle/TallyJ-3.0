using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class VoteCacher : CacherBase<Vote>
  {
    protected override IQueryable<Vote> MainQuery()
    {
      return CurrentDb.Vote
        .Join(CurrentDb.Ballot, v => v.BallotGuid, b => b.BallotGuid, (v, b) => new { v, b })
        .Join(CurrentDb.Location.Where(l => l.ElectionGuid == UserSession.CurrentElectionGuid), g => g.b.LocationGuid,
          l => l.LocationGuid, (g, l) => g.v);
    }

    protected override void ItemChanged()
    {
      new ResultSummaryCacher().VoteOrPersonChanged();
    }

    private static object _lockObject;
    protected override object LockCacheBaseObject
    {
      get
      {
        return _lockObject ?? (_lockObject = new object());
      }
    }

  }
}