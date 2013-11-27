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
  public partial class Teller
  {
    /// <summary>
    ///   Get all Tellers for this election
    /// </summary>
    public static IEnumerable<Teller> AllTellersCached
    {
      get
      {
        var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

        if (db.IsFaked) throw new ApplicationException("Can't be used in tests");

        return db.Tellers.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid)
          .FromCache(null, new[] { "AllTellers" + UserSession.CurrentElectionGuid });
      }
    }

    /// <summary>
    ///   Drop the cache of Tellers for this election
    /// </summary>
    public static void DropCachedTellers()
    {
      if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;

      CacheManager.Current.Expire("AllTellers" + UserSession.CurrentElectionGuid);
    }
  }
}