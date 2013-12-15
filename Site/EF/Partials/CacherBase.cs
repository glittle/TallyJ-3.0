using System;
using System.Collections.Generic;
using System.Linq;
using EntityFramework;
using EntityFramework.Caching;
using EntityFramework.Extensions;
using TallyJ.Code.Data;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.EF
{
  public abstract class CacherBase
  {
    /// <summary>
    ///   Remove all cached data for this election
    /// </summary>
    public static void DropAllCachesForThisElection()
    {
      if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;
      CacheManager.Current.Expire(UserSession.CurrentElectionGuid.ToString());
    }
  }

  public abstract class CacherBase<T> : CacherBase where T : class, IIndexedForCaching
  {
    /// <summary>
    ///   The key for the current election's data
    /// </summary>
    protected virtual string CacheKeyRaw
    {
      get { return typeof(T).Name + UserSession.CurrentElectionGuid; }
    }

    /// <summary>
    /// Get a single <typeparamref name="T"/> by Id. If not found, returns null
    /// </summary>
    /// <param name="rowId"></param>
    /// <returns></returns>
    public T GetById(int rowId)
    {
      return AllForThisElection.FirstOrDefault(t => t.C_RowId == rowId);
    }

    public List<T> AllForThisElection
    {
      get
      {
        var db = CurrentDb;

        if (db.IsFaked) throw new ApplicationException("Can't be used in tests");

        CacheKey internalCacheKey;

        var allForThisElection = MainQuery(db)
          .FromCache(out internalCacheKey, CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(60)),
            new[] { CacheKeyRaw, UserSession.CurrentElectionGuid.ToString() });


        if (typeof(T) == typeof(Election))
        {
          RegisterElectionCacheKey(internalCacheKey);
        }

        return allForThisElection;
      }
    }

    protected const string ElectionKeys = "ElectionKeys";

    private void RegisterElectionCacheKey(CacheKey electionCacheKey)
    {
      var cacheManager = Locator.Current.Resolve<CacheManager>();

      var list =
        cacheManager.GetOrAdd(ElectionKeys, new List<CacheKey>(), CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(60))) as
          List<CacheKey>;
      if (list == null)
      {
        return;
      }
      
      if (!list.Exists(k => k.Key == electionCacheKey.Key))
      {
        list.Add(electionCacheKey);
        cacheManager.Set(ElectionKeys, list, CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(60)));
      }
    }


    private TallyJ2dEntities CurrentDb
    {
      get { return UnityInstance.Resolve<IDbContextFactory>().DbContext; }
    }

    /// <summary>
    ///   Find the item by matching the _RowId (if found), remove it, then replace it with this one
    /// </summary>
    /// <param name="replacementItem"></param>
    public void UpdateItemAndSaveCache(T replacementItem)
    {
      var list = AllForThisElection;

      var oldItems = list.Where(i => i.C_RowId == replacementItem.C_RowId);
      foreach (var item in oldItems.ToList())
      {
        list.Remove(item);
      }

      list.Add(replacementItem);

      ReplaceEntireCache(list);
    }

    /// <summary>
    ///   Find the item by matching the _RowId, then replace it with this one
    /// </summary>
    /// <param name="itemToRemove"></param>
    public void RemoveItemAndSaveCache(T itemToRemove)
    {
      var list = AllForThisElection;
      var removed = false;

      var oldItems = list.Where(i => i.C_RowId == itemToRemove.C_RowId).ToList();
      foreach (var item in oldItems)
      {
        list.Remove(item);
        removed = true;
      }

      if (removed)
      {
        ReplaceEntireCache(list);
      }
    }

    /// <summary>
    ///   Put the (modified) List back into the cache
    /// </summary>
    /// <param name="listFromCache"></param>
    public void ReplaceEntireCache(List<T> listFromCache)
    {
      var key = new CacheKey(MainQuery(CurrentDb).GetCacheKey(), 
        new[] { CacheKeyRaw, UserSession.CurrentElectionGuid.ToString() });

      Locator.Current.Resolve<CacheManager>()
        .Set(key, listFromCache, CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(60)));
    }

    /// <summary>
    ///   Add this item to the cached list. The cache is updated with the current version of the data.
    /// </summary>
    /// <param name="newItem"></param>
    /// <returns></returns>
    public T AddItemAndSaveCache(T newItem)
    {
      var list = AllForThisElection;

      AssertAtRuntime.That(!list.Exists(i => i.C_RowId == newItem.C_RowId), "Can't add existing item");
      AssertAtRuntime.That(newItem.C_RowId != 0, "Can't add if id is 0");

      list.Add(newItem);
      ReplaceEntireCache(list);
      return newItem;
    }

    /// <summary>
    ///   Drop the cache of
    ///   <typeparam name="T"></typeparam>
    ///   for this election
    /// </summary>
    public void DropThisCache()
    {
      if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;
      CacheManager.Current.Expire(CacheKeyRaw);
    }

    protected abstract IQueryable<T> MainQuery(TallyJ2dEntities db);
  }
}