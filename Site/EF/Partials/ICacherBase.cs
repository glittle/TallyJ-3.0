using System.Collections.Generic;

namespace TallyJ.EF
{
  public interface ICacherBase<T> where T : class, IIndexedForCaching
  {
    /// <summary>
    /// Get a single <typeparamref name="T"/> by Id. If not found, returns null
    /// </summary>
    /// <param name="rowId"></param>
    /// <returns></returns>
    T GetById(int rowId);

    List<T> AllForThisElection { get; }

    /// <summary>
    /// Find the item by matching the _RowId (if found), remove it, then replace it with this one. 
    /// Can be used to Add or Update
    /// </summary>
    /// <param name="replacementItem"></param>
    ICacherBase<T> UpdateItemAndSaveCache(T replacementItem);

    /// <summary>
    ///   Find the item by matching the _RowId, remove if found
    /// </summary>
    /// <param name="itemToRemove"></param>
    ICacherBase<T> RemoveItemAndSaveCache(T itemToRemove);

    void RemoveItemsAndSaveCache(IEnumerable<T> itemsToRemove);

    /// <summary>
    ///   Put the (modified) List back into the cache
    /// </summary>
    /// <param name="listFromCache"></param>
    void ReplaceEntireCache(List<T> listFromCache);

    /// <summary>
    ///   Drop the cache of
    ///   <typeparam name="T"></typeparam>
    ///   for this election
    /// </summary>
    ICacherBase<T> DropThisCache();
  }
}