using System;
using System.Collections.Generic;
using System.Linq;
using EntityFramework.Caching;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public abstract class NonDbCacherBase
  {
    /// <summary>
    ///   Remove all cached data for this election
    /// </summary>
    public static void DropAllCachesForThisElection()
    {
      CacheManager.Current.Expire(UserSession.CurrentElectionGuid.ToString());
    }
  }

  public abstract class NonDbCacherBase<T> where T : class, IIndexedForCaching
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

    protected IDictionary<int, T> CachedDict
    {
      get
      {
        return CacheManager.Current.Get(CacheKeyRaw) as IDictionary<int, T>
               ?? new Dictionary<int, T>();
      }
    }

    public List<T> AllForThisElection
    {
      get { return CachedDict.Values.ToList(); }
    }

    public T GetById(int rowId)
    {
      lock (_thisLock)
      {
        return CachedDict.ContainsKey(rowId) ? CachedDict[rowId] : null;
      }
    }

    public void UpdateItemAndSaveCache(T replacementItem)
    {
      AssertAtRuntime.That(replacementItem.C_RowId != 0, "Can't add if id is 0");

      lock (_thisLock)
      {
        var list = CachedDict;

        list[replacementItem.C_RowId] = replacementItem;

        ReplaceEntireCache(list);
      }
    }

    public void RemoveItemAndSaveCache(T itemToRemove)
    {
      lock (_thisLock)
      {
        var list = CachedDict;

        if (list.ContainsKey(itemToRemove.C_RowId))
        {
          list.Remove(itemToRemove.C_RowId);
          ReplaceEntireCache(list);
        }
      }
    }

    public void RemoveItemsAndSaveCache(IEnumerable<T> itemsToRemove)
    {
      lock (_thisLock)
      {
        var list = CachedDict;
        var removed = false;

        foreach (var item in itemsToRemove.Where(item => list.ContainsKey(item.C_RowId)))
        {
          list.Remove(item.C_RowId);
          removed = true;
        }

        if (removed)
        {
          ReplaceEntireCache(list);
        }
      }
    }

    public void ReplaceEntireCache(IDictionary<int, T> listForCache)
    {
      var key = new CacheKey(CacheKeyRaw,
        new[] { UserSession.CurrentElectionGuid.ToString() });

      CacheManager.Current.Set(key, listForCache, CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(CacheMinutes)));

      ItemChanged();
    }

    public void DropThisCache()
    {
      CacheManager.Current.Expire(CacheKeyRaw);
    }

    protected virtual void ItemChanged() { }
  }
}