using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class TellerCacher : CacherBase<Teller>
  {
    public override IQueryable<Teller> MainQuery()
    {
      return CurrentDb.Teller.Where(p => p.ElectionGuid == CurrentElectionGuid);
    }

    private static object _lockObject;

    public TellerCacher(ITallyJDbContext dbContext) : base(dbContext)
    {
    }
    public TellerCacher() : base(UserSession.DbContext)
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