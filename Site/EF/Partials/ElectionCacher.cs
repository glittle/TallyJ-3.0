using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ElectionCacher : CacherBase<Election>
  {
    protected override IQueryable<Election> MainQuery(TallyJ2dEntities db)
    {
      return db.Election.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid);
    }
  }
}