using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ResultTieCacher : CacherBase<ResultTie>
  {
    protected override IQueryable<ResultTie> MainQuery(TallyJ2dEntities db)
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      return db.ResultTie.Where(p => p.ElectionGuid == currentElectionGuid);
    }
  }
}