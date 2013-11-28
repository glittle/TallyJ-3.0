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
  public interface IBallotBase
  {
    System.Guid BallotGuid { get; set; }
    string StatusCode { get; set; }
  }

  public partial class Ballot : IBallotBase
  {
    /// <summary>
    ///   Get all Ballots for this election
    /// </summary>
    public static IEnumerable<Ballot> AllBallotsCached
    {
      get
      {
        var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

        if (db.IsFaked) throw new ApplicationException("Can't be used in tests");

        return db.Ballots
          .Join(Location.AllLocationsCached, b => b.LocationGuid, l => l.LocationGuid, (b, l) => b)
          .FromCache(null, new[] { "AllBallots" + UserSession.CurrentElectionGuid });
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
  public partial class vBallotInfo : IBallotBase
  {

  }
}