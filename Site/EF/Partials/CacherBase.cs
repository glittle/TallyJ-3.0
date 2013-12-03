using System;
using System.Collections.Generic;
using System.Linq;
using EntityFramework;
using EntityFramework.Caching;
using EntityFramework.Extensions;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.EF
{
  public abstract class CacherBase<T> where T : class
  {
    /// <summary>
    /// Put the List back into the cache
    /// </summary>
    /// <param name="listFromCache"></param>
    public void UpdateCache(List<T> listFromCache)
    {
      var key = new CacheKey(MainQuery(CurrentDb).GetCacheKey(), new[] { CacheKey });
      
      Locator.Current.Resolve<CacheManager>().Set(key, listFromCache, CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(60)));
    }

    /// <summary>
    ///   Drop the cache of Votes for this election
    /// </summary>
    public void DropCached()
    {
      if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;
      CacheManager.Current.Expire(CacheKey);
    }

    /// <summary>
    /// The key for the current election's data
    /// </summary>
    protected virtual string CacheKey
    {
      get { return typeof(T).Name + UserSession.CurrentElectionGuid; }
    }

    protected abstract IQueryable<T> MainQuery(TallyJ2dEntities db);

    public List<T> AllForThisElection
    {
      get
      {
        var db = CurrentDb;

        if (db.IsFaked) throw new ApplicationException("Can't be used in tests");

        return MainQuery(db).FromCache(CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(60)), new[] { CacheKey });
      }
    }

    private static TallyJ2dEntities CurrentDb
    {
      get { return UnityInstance.Resolve<IDbContextFactory>().DbContext; }
    }
  }
}