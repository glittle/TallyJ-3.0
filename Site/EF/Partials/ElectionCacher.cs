using System;
using System.Collections.Generic;
using System.Linq;
using EntityFramework;
using EntityFramework.Caching;
using EntityFramework.Extensions;
using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ElectionCacher : CacherBase<Election>
  {
    protected override IQueryable<Election> MainQuery(TallyJ2dEntities db)
    {
      return db.Election.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid);
    }

    /// <summary>
    /// All elections that are currently cached
    /// </summary>
    public List<PublicElection> PublicElections
    {
      get
      {
        var result = new List<PublicElection>();

        var cacheManager = Locator.Current.Resolve<CacheManager>();
        var electionKeys = cacheManager.Get(ElectionKeys) as List<CacheKey>;
        if (electionKeys != null)
        {
          foreach (var key in electionKeys)
          {
            var cached = cacheManager.Get(key) as List<Election>;
            if (cached != null)
            {
              result.AddRange(cached.Where(e => e.ListForPublic.AsBoolean()
                                                && e.ElectionPasscode.HasContent()
                                                && DateTime.Now - e.ListedForPublicAsOf <= 5.minutes()
                ).Select(e => new PublicElection(e)));
            }
          }
        }
        return result;
      }
    }
  }
}