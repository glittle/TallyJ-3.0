using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class LocationCacher : CacherBase<Location>
  {
    public override IQueryable<Location> MainQuery()
    {
      return CurrentDb.Location.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid);
    }

    private static object _lockObject;

    public LocationCacher(ITallyJDbContext dbContext) : base(dbContext)
    {
    }
    public LocationCacher() : base(UserSession.DbContext)
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