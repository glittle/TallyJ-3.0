using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ResultSummaryCacher : CacherBase<ResultSummary>
  {
    protected override IQueryable<ResultSummary> MainQuery()
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      return CurrentDb.ResultSummary.Where(p => p.ElectionGuid == currentElectionGuid);
    }

    public void ClearOldResults()
    {
    }
  }
}