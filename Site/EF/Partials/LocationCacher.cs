using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class LocationCacher : CacherBase<Location>
  {
    protected override IQueryable<Location> MainQuery(TallyJ2dEntities db)
    {
      return db.Location.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid);
    }
  }
}