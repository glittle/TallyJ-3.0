using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ResultCacher : CacherBase<Result>
  {
    protected override IQueryable<Result> MainQuery()
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      return CurrentDb.Result.Where(p => p.ElectionGuid == currentElectionGuid);
    }
  }
}