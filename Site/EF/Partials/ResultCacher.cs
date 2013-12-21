using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ResultCacher : CacherBase<Result>
  {
    protected override IQueryable<Result> MainQuery(TallyJ2dEntities db)
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      return db.Result.Where(p => p.ElectionGuid == currentElectionGuid);
    }
  }
}