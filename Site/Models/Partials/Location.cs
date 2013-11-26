using System;
using System.Collections.Generic;
using System.Linq;
using EntityFramework.Caching;
using EntityFramework.Extensions;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.Models
{
  public partial class Location
  {
    /// <summary>
    ///   Get all Locations for this election
    /// </summary>
    public static IEnumerable<Location> AllLocationsCached
    {
      get
      {
        var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

        if (db.IsFaked) throw new ApplicationException("Can't be used in tests");

        return db.Locations.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid)
          .FromCache(null, new[] {"AllLocations" + UserSession.CurrentElectionGuid});
      }
    }

    /// <summary>
    ///   Drop the cache of Locations for this election
    /// </summary>
    public static void DropCachedLocations()
    {
      if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;

      CacheManager.Current.Expire("AllLocations" + UserSession.CurrentElectionGuid);
    }
  }
}