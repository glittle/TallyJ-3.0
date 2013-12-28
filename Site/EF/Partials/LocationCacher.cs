using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class LocationCacher : CacherBase<Location>
  {
    protected override IQueryable<Location> MainQuery()
    {
      return CurrentDb.Location.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid);
    }
  }
}