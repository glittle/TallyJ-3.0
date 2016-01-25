using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ResultTieCacher : CacherBase<ResultTie>
  {
    public override IQueryable<ResultTie> MainQuery()
    {
      return CurrentDb.ResultTie.Where(p => p.ElectionGuid == CurrentElectionGuid);
    }

    private static object _lockObject;

    public ResultTieCacher(ITallyJDbContext dbContext) : base(dbContext)
    {
    }
    public ResultTieCacher() : base(UserSession.DbContext)
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