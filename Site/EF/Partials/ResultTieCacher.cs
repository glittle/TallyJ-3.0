using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ResultTieCacher : CacherBase<ResultTie>
  {
    protected override IQueryable<ResultTie> MainQuery()
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      return CurrentDb.ResultTie.Where(p => p.ElectionGuid == currentElectionGuid);
    }
  }
}