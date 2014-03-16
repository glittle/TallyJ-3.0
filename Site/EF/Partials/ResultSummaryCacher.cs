using System.Data;
using System.Linq;
using EntityFramework.Extensions;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;

namespace TallyJ.EF
{
  public class ResultSummaryCacher : CacherBase<ResultSummary>
  {
    protected override IQueryable<ResultSummary> MainQuery()
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      return CurrentDb.ResultSummary.Where(p => p.ElectionGuid == currentElectionGuid);
    }

    public void VoteOrPersonChanged()
    {
      var results = AllForThisElection;
      if (results.Any(r => r.ResultType != ResultType.Manual))
      {
        CurrentDb.ResultSummary.Delete(r => r.ResultType != ResultType.Manual);
        results.RemoveAll(r => r.ResultType != ResultType.Manual);
        ReplaceEntireCache(results);
      }
    }

    private static object _lockObject;
    protected override object LockCacheBaseObject
    {
      get
      {
        return _lockObject ?? (_lockObject = new object());
      }
    }

  }
}