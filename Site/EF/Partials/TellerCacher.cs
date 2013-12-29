using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class TellerCacher : CacherBase<Teller>
  {
    protected override IQueryable<Teller> MainQuery()
    {
      return CurrentDb.Teller.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid);
    }
  }
}