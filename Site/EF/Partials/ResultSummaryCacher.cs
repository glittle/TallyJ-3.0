using System.Data;
using System.Linq;
using EntityFramework.Extensions;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;

namespace TallyJ.EF
{
  public class ResultSummaryCacher : CacherBase<ResultSummary>
  {
    public override IQueryable<ResultSummary> MainQuery()
    {
      return CurrentDb.ResultSummary.Where(p => p.ElectionGuid == CurrentElectionGuid);
    }

    public void VoteOrPersonChanged()
    {
      var results = AllForThisElection;
      if (results.Any(r => r.ResultType != ResultType.Manual))
      {
        CurrentDb.ResultSummary.Where(r => r.ResultType != ResultType.Manual).Delete();
        results.RemoveAll(r => r.ResultType != ResultType.Manual);
        ReplaceEntireCache(results);
      }
    }

    private static object _lockObject;

    public ResultSummaryCacher(ITallyJDbContext dbContext) : base(dbContext)
    {
    }
    public ResultSummaryCacher() : base(UserSession.DbContext)
    {
    }

    protected override object LockCacheBaseObject
    {
      get
      {
        return _lockObject ?? (_lockObject = new object());
      }
    }

  }
}