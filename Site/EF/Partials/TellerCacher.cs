using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class TellerCacher : CacherBase<Teller>
  {
    protected override IQueryable<Teller> MainQuery(TallyJ2dEntities db)
    {
      return db.Teller.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid);
    }
  }
}