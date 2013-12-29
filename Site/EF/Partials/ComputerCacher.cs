using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ComputerCacher : CacherBase<Computer>
  {
    protected override IQueryable<Computer> MainQuery()
    {
      return CurrentDb.Computer.Where(c => c.ElectionGuid == UserSession.CurrentElectionGuid);
    }
  }
}