using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ElectionCacher : CacherBase<Election>
  {
    public override IQueryable<Election> MainQuery()
    {
      return CurrentDb.Election.Where(p => p.ElectionGuid == CurrentElectionGuid);
    }

    /// <summary>
    /// All elections that are currently cached
    /// </summary>
//    public List<PublicElection> PublicElections
//    {
//      get
//      {
//        var result = new List<PublicElection>();
//
//        var cacheManager = Locator.Current.Resolve<CacheManager>();
//        var electionKeys = new PublicElectionLister().CacheKeysForElectionsToList;
//        foreach (var key in electionKeys) //ensure we use a copy?
//        {
//          var cached = cacheManager.Get(key) as List<Election>;
//          if (cached != null)
//          {
//            result.AddRange(cached.Where(e => e.ListForPublicCalculated).Select(e => new PublicElection(e)));
//          }
//        }
//        return result;
//      }
//    }

    private static object _lockObject;

    public ElectionCacher(ITallyJDbContext dbContext) : base(dbContext)
    {
    }
    public ElectionCacher() : base(UserSession.GetNewDbContext)
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