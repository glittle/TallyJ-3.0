using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ResultCacher : CacherBase<Result>
  {
    public override IQueryable<Result> MainQuery()
    {
      return CurrentDb.Result.Where(p => p.ElectionGuid == CurrentElectionGuid);
    }

    private static object _lockObject;

    public ResultCacher(ITallyJDbContext dbContext) : base(dbContext)
    {
    }
    public ResultCacher() : base(UserSession.DbContext)
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