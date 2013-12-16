using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ResultSummaryCacher : CacherBase<ResultSummary>
  {
    protected override IQueryable<ResultSummary> MainQuery(TallyJ2dEntities db)
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      return db.ResultSummary.Where(p => p.ElectionGuid == currentElectionGuid);
    }
  }
}