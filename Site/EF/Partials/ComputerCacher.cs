using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ComputerCacher : CacherBase<Computer>
  {
    protected override IQueryable<Computer> MainQuery(TallyJ2dEntities db)
    {
      return db.Computer.Where(c => c.ElectionGuid == UserSession.CurrentElectionGuid);
    }
  }
}