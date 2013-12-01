using System;
using System.Collections.Generic;
using System.Linq;
using EntityFramework.Caching;
using EntityFramework.Extensions;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.EF
{
  public partial class Ballot
  {
    public long RowVersionInt {
      get
      {
        return BitConverter.ToInt64(C_RowVersion, 0);
      }
    }

    /// <summary>
    ///   Get all Ballots for this election
    /// </summary>
    public static IEnumerable<Ballot> AllBallotsCached
    {
      get
      {
        var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

        if (db.IsFaked) throw new ApplicationException("Can't be used in tests");

        var locationGuids = Location.AllLocationsCached.Select(l => l.LocationGuid).ToList();

        return db.Ballot.Where(b => locationGuids.Contains(b.LocationGuid))
          .FromCache(CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(60)), new[] { "AllBallots" + UserSession.CurrentElectionGuid });
      }
    }

    /// <summary>
    ///   Drop the cache of Ballots for this election
    /// </summary>
    public static void DropCachedBallots()
    {
      if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;

      CacheManager.Current.Expire("AllBallots" + UserSession.CurrentElectionGuid);
    }
  }
}