using System;
using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class BallotCacher : CacherBase<Ballot>
  {
    protected override IQueryable<Ballot> MainQuery()
    {
      return CurrentDb.Ballot
        .Join(CurrentDb.Location.Where(l => l.ElectionGuid == UserSession.CurrentElectionGuid), b => b.LocationGuid, l => l.LocationGuid, (b, l) => b);
    }

    protected override void ItemChanged()
    {
      new ResultSummaryCacher().VoteOrPersonChanged();
    }
  }
}