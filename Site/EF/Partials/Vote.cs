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
  public partial class Vote
  {
    /// <summary>
    ///   Get all Votes for this election
    /// </summary>
    public static IEnumerable<Vote> AllVotesCached
    {
      get
      {
        var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

        if (db.IsFaked) throw new ApplicationException("Can't be used in tests");

        return db.Vote
          .Join(db.Ballot, v => v.BallotGuid, b => b.BallotGuid, (v, b) => new { v, b })
          .Join(db.Location.Where(l => l.ElectionGuid == UserSession.CurrentElectionGuid), g => g.b.LocationGuid, l => l.LocationGuid, (g, l) => g.v)
          .FromCache(CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(60)), new[] { "AllVotes" + UserSession.CurrentElectionGuid });
      }
    }

    /// <summary>
    ///   Drop the cache of Votes for this election
    /// </summary>
    public static void DropCachedVotes()
    {
      if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;

      CacheManager.Current.Expire("AllVotes" + UserSession.CurrentElectionGuid);
    }
  }
}