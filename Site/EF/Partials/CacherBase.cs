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

  public abstract class CacherBase<T> : CacherBase, ICacherBase<T> where T : class, IIndexedForCaching
  {
    private const int CacheMinutes = 180; // 3 hours
    private readonly object _thisLock = new object();

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

        List<T> allForThisElection;
        lock (_thisLock)
        {
          allForThisElection = MainQuery()
            .FromCache(out internalCacheKey, CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(CacheMinutes)),
              new[] { CacheKeyRaw, UserSession.CurrentElectionGuid.ToString() });
        }

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
      var cacheManager = CacheManager.Current;

      var cacheDuration = TimeSpan.FromMinutes(CacheMinutes);
      var list =
        cacheManager.GetOrAdd(ElectionKeys, new List<CacheKey>(), CachePolicy.WithSlidingExpiration(cacheDuration)) as
          List<CacheKey>;
      if (list == null)
      {
        return;
      }

      if (!list.Exists(k => k.Key == electionCacheKey.Key))
      {
        list.Add(electionCacheKey);
      }
      cacheManager.Set(ElectionKeys, list, CachePolicy.WithSlidingExpiration(cacheDuration));
    }


    protected TallyJ2dEntities CurrentDb
    {
      get { return UnityInstance.Resolve<IDbContextFactory>().DbContext; }
    }

    /// <summary>
    /// Find the item by matching the _RowId (if found), remove it, then replace it with this one. 
    /// Can be used to Add or Update
    /// </summary>
    /// <param name="replacementItem"></param>
    public void UpdateItemAndSaveCache(T replacementItem)
    {
      AssertAtRuntime.That(replacementItem.C_RowId != 0, "Can't add if id is 0");

      lock (_thisLock)
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
    }


    public void RemoveItemsAndSaveCache(IEnumerable<T> itemsToRemove)
    {
      var removed = false;

      var ids = itemsToRemove.Select(i => i.C_RowId).ToList();

      lock (_thisLock)
      {
        var list = AllForThisElection;
        var oldItems = list.Where(i => ids.Contains(i.C_RowId)).ToList();
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
    }

    /// <summary>
    ///   Find the item by matching the _RowId, remove if found
    /// </summary>
    /// <param name="itemToRemove"></param>
    public void RemoveItemAndSaveCache(T itemToRemove)
    {
      var removed = false;
      lock (_thisLock)
      {
        var list = AllForThisElection;
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
    }

    /// <summary>
    ///   Put the (modified) List back into the cache
    /// </summary>
    /// <param name="listFromCache"></param>
    public void ReplaceEntireCache(List<T> listFromCache)
    {
      var key = new CacheKey(MainQuery().GetCacheKey(),
        new[] { CacheKeyRaw, UserSession.CurrentElectionGuid.ToString() });

      CacheManager.Current.Set(key, listFromCache, CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(CacheMinutes)));

      ItemChanged();
    }

    /// <summary>
    ///   Add this item to the cached list. The cache is updated with the current version of the data.
    /// </summary>
    /// <param name="newItem"></param>
    /// <returns></returns>
    //public T AddItemAndSaveCache(T newItem)
    //{
    //  var list = AllForThisElection;

    //  AssertAtRuntime.That(!list.Exists(i => i.C_RowId == newItem.C_RowId), "Can't add existing item");
    //  AssertAtRuntime.That(newItem.C_RowId != 0, "Can't add if id is 0");

    //  list.Add(newItem);
    //  ReplaceEntireCache(list);
    //  return newItem;
    //}

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

    /// <summary>
    /// When an item has been added or changed
    /// </summary>
    protected virtual void ItemChanged() { }

    protected abstract IQueryable<T> MainQuery();
  }
}