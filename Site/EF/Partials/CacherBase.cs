using EntityFramework.Caching;
using EntityFramework.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public abstract class CacherBase
  {
    private Guid _currentElectionGuid;
    private Guid _currentPeopleElectionGuid;
    protected abstract object LockCacheBaseObject { get; }

    public Guid CurrentElectionGuid =>
      _currentElectionGuid != Guid.Empty
        ? _currentElectionGuid
        : (_currentElectionGuid = UserSession.CurrentElectionGuid);

    public Guid CurrentPeopleElectionGuid =>
      _currentPeopleElectionGuid != Guid.Empty
        ? _currentPeopleElectionGuid
        : (_currentPeopleElectionGuid = UserSession.CurrentPeopleElectionGuid);
  }

  public abstract class CacherBase<T> : CacherBase, ICacherBase<T> where T : class, IIndexedForCaching
  {
    private const int CacheMinutes = 30; // long enough for a reasonable gap in usage

    protected CacherBase(ITallyJDbContext dbContext)
    {
      CurrentDb = dbContext;
    }

    /// <summary>
    ///   The key for the current election's data
    /// </summary>
    protected virtual string CacheKeyRaw => typeof(T).Name + CurrentElectionGuid;// + CurrentPeopleElectionGuid;

    protected ITallyJDbContext CurrentDb { get; set; }

    /// <summary>
    ///   Get a single <typeparamref name="T" /> by Id. If not found, returns null
    /// </summary>
    /// <param name="rowId"></param>
    /// <returns></returns>
    public T GetById(int rowId)
    {
      return AllForThisElection.FirstOrDefault(t => t.C_RowId == rowId);
    }

    public virtual List<T> AllForThisElection
    {
      get
      {
        List<T> allForThisElection;
        lock (LockCacheBaseObject)
        {
          allForThisElection = MainQuery()
            .FromCache(CachePolicy.WithSlidingExpiration(TimeSpan.FromMinutes(CacheMinutes)),
              new[] { CacheKeyRaw, CurrentElectionGuid.ToString() }).ToList();
        }

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
        new[] { CacheKeyRaw, CurrentElectionGuid.ToString() });

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
    ///   Expire this cache so will be refreshed on next use
    /// </summary>
    /// <returns></returns>
    public ICacherBase<T> DropThisCache()
    {
      CacheManager.Current.Expire(CacheKeyRaw);
      return this;
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