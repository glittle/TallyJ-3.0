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
  [Serializable]
  public partial class Election
  {
    public bool IsSingleNameElection
    {
      get { return NumberToElect.GetValueOrDefault(0) == 1 && NumberExtra.GetValueOrDefault(0) == 0; }
    }

    /// <Summary>Erase all ballots and results</Summary>
    public static void EraseBallotsAndResults(Guid electionGuid)
    {
      var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

      db.Result.Delete(r => r.ElectionGuid == electionGuid);
      db.ResultTie.Delete(r => r.ElectionGuid == electionGuid);
      db.ResultSummary.Delete(r => r.ElectionGuid == electionGuid);

      // delete ballots in all locations... cascading will delete votes
      db.Ballot.Delete(b => Location.AllLocationsCached.Select(l => l.LocationGuid).Contains(b.LocationGuid));
    }



    /// <summary>
    /// Get all people for this election
    /// </summary>
    public static Election ThisElectionCached
    {
      get
      {
        var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

        if (db.IsFaked) throw new ApplicationException("Can't be used in tests");

        var currentElectionGuid = UserSession.CurrentElectionGuid;

        return db.Election.Where(p => p.ElectionGuid == currentElectionGuid).FromCache(CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(60)), new[] { "ThisElection" + currentElectionGuid }).First();
      }
    }
    /// <summary>
    /// Drop the cache of people for this election
    /// </summary>
    public static void DropCachedElection()
    {
      if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;

      CacheManager.Current.Expire("ThisElection" + UserSession.CurrentElectionGuid);
    }
  }
}