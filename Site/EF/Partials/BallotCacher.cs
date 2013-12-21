using System;
using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class BallotCacher : CacherBase<Ballot>
  {
    protected override IQueryable<Ballot> MainQuery(TallyJ2dEntities db)
    {
      return db.Ballot
        .Join(db.Location.Where(l => l.ElectionGuid == UserSession.CurrentElectionGuid), b => b.LocationGuid, l => l.LocationGuid, (b, l) => b);
    }

  }
}