using System;
using System.Collections.Generic;
using System.Linq;
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
    private Guid _currentElectionGuid;
    protected abstract object LockCacheBaseObject { get; }

    public Guid CurrentElectionGuid
    {
      get
      {
        return _currentElectionGuid != Guid.Empty
          ? _currentElectionGuid
          : (_currentElectionGuid = UserSession.CurrentElectionGuid);
      }
    }

  }

  public abstract class CacherBase<T> : CacherBase, ICacherBase<T> where T : class, IIndexedForCaching
  {
    private const int CacheMinutes = 180; // 3 hours

    /// <summary>
    ///   The key for the current election's data
    /// </summary>
    protected virtual string CacheKeyRaw
    {
      get { return typeof (T).Name + CurrentElectionGuid; }
    }

    protected TallyJ2dEntities CurrentDb
    {
      get { return UnityInstance.Resolve<IDbContextFactory>().DbContext; }
    }

    /// <summary>
    ///   Get a single <typeparamref name="T" /> by Id. If not found, returns null
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

        List<T> allForThisElection;
        lock (LockCacheBaseObject)
        {
          //CacheKey internalCacheKey;
          allForThisElection = MainQuery()
            .FromCache(CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(CacheMinutes)),
              new[] {CacheKeyRaw, CurrentElectionGuid.ToString()}).ToList();
        }

        //        if (typeof(T) == typeof(Election))
        //        {
        //          new PublicElectionLister().up.RegisterElectionCacheKey(internalCacheKey);
        //        }

        return allForThisElection;
      }
    }

    //    protected const string ElectionKeys = "ElectionKeys";


    /// <summary>
    ///   Find the item by matching the _RowId (if found), remove it, then replace it with this one.
    ///   Can be used to Add or Update
    /// </summary>
    /// <param name="replacementItem"></param>
    public ICacherBase<T> UpdateItemAndSaveCache(T replacementItem)
    {
      AssertAtRuntime.That(replacementItem.C_RowId != 0, "Can't add if id is 0");

      lock (LockCacheBaseObject)
      {
        var list = AllForThisElection;

        var oldItem = list.FirstOrDefault(i => i.C_RowId == replacementItem.C_RowId);
        if (oldItem != null)
        {
          list.Remove(oldItem);
        }

        list.Add(replacementItem);

        ReplaceEntireCache(list);
      }

      return this;
    }


    public void RemoveItemsAndSaveCache(IEnumerable<T> itemsToRemove)
    {
      var removed = false;

      var ids = itemsToRemove.Select(i => i.C_RowId).ToList();

      lock (LockCacheBaseObject)
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
    public ICacherBase<T> RemoveItemAndSaveCache(T itemToRemove)
    {
      lock (LockCacheBaseObject)
      {
        var list = AllForThisElection;
        var oldItem = list.FirstOrDefault(i => i.C_RowId == itemToRemove.C_RowId);
        if (oldItem != null)
        {
          list.Remove(oldItem);
          ReplaceEntireCache(list);
        }
      }
      return this;
    }

    /// <summary>
    ///   Put the (modified) List back into the cache
    /// </summary>
    /// <param name="listFromCache"></param>
    public void ReplaceEntireCache(List<T> listFromCache)
    {
      var key = new CacheKey(MainQuery().GetCacheKey(),
        new[] {CacheKeyRaw, CurrentElectionGuid.ToString()});

      CacheManager.Current.Set(key, listFromCache, CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(CacheMinutes)));

      ItemChanged();
    }

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
    ///   Add this item to the cached list. The cache is updated with the current version of the data.
    /// </summary>
    /// <param name="newItem"></param>
    /// <returns></returns>
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
    ///   When an item has been added or changed
    /// </summary>
    protected virtual void ItemChanged()
    {
    }

    public abstract IQueryable<T> MainQuery();
  }
}