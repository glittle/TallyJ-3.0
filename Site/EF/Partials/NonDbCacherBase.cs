//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using EntityFramework.Caching;
//using TallyJ.Code.Helpers;
//using TallyJ.Code.Session;
//
//namespace TallyJ.EF
//{
//  /// <summary>
//  /// This static cache is shared across all elections in active use!
//  /// </summary>
//  /// <typeparam name="T"></typeparam>
//  public abstract class NonDbCacherBase<T> where T : class, IIndexedForCaching
//  {
//    private readonly static ConcurrentDictionary<int, T> CachedDict = new ConcurrentDictionary<int, T>();
//
//    public List<T> AllForThisElection
//    {
//      get
//      {
//        return CachedDict.Values.Where(x=>).ToList();
//      }
//    }
//
//    public T GetById(int rowId)
//    {
//      return CachedDict.ContainsKey(rowId) ? CachedDict[rowId] : null;
//    }
//
//    public void UpdateItemAndSaveCache(T replacementItem)
//    {
//      AssertAtRuntime.That(replacementItem.C_RowId != 0, "Can't add if id is 0");
//
//      var list = CachedDict;
//
//      list[replacementItem.C_RowId] = replacementItem;
//
//    }
//
//    public void RemoveItemAndSaveCache(T itemToRemove)
//    {
//      var list = CachedDict;
//      if (!list.ContainsKey(itemToRemove.C_RowId)) return;
//
//      T removed;
//      list.TryRemove(itemToRemove.C_RowId, out removed);
//    }
//
//    public void RemoveItemsAndSaveCache(IEnumerable<T> itemsToRemove)
//    {
//      var list = CachedDict;
//
//      foreach (var item in itemsToRemove.Where(item => list.ContainsKey(item.C_RowId)))
//      {
//        list.Remove(item.C_RowId);
//      }
//    }
//
//    protected virtual void ItemChanged() { }
//  }
//}