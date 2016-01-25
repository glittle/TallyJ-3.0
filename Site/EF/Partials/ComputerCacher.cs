using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Hubs;

namespace TallyJ.EF
{
  public class ComputerCacher // not extending the base cacher
  {
    /// This static cache is shared across all elections in active use!
    private static readonly ConcurrentDictionary<Guid, Computer> CachedDict = new ConcurrentDictionary<Guid, Computer>();
    private ITallyJDbContext db;

    public ComputerCacher(ITallyJDbContext db)
    {
      this.db = db;
    }
    public ComputerCacher()
    {
      this.db = UserSession.DbContext;
    }

    public List<Computer> AllForThisElection
    {
      get { return CachedDict.Values.Where(c => c.ElectionGuid == UserSession.CurrentElectionGuid).ToList(); }
    }

    /// <summary>
    ///   Add this new computer to the cache
    /// </summary>
    /// <param name="computer"></param>
    public void AddToCache(Computer computer)
    {
      var wasAdded = CachedDict.TryAdd(computer.ComputerGuid, computer);
      AssertAtRuntime.That(wasAdded, "can't add!");

      new PublicElectionLister().UpdateThisElectionInList();
    }



    /// <summary>
    /// </summary>
    /// <param name="computer"></param>
    //    public void UpdateItemAndSaveCache(Computer computer)
    //    {
    //      computer.LastContact = DateTime.Now;
    //      var wasReplaced = CachedDict.TryUpdate(computer.ComputerGuid, computer, computer);
    //      CachedDict.AddOrUpdate(computer.ComputerGuid, computer, (guid, computer1) =>
    //      {
    //        
    //      });
    //    }
    public void RemoveItemAndSaveCache(Computer itemToRemove)
    {
      Computer removed;
      var wasRemoved = CachedDict.TryRemove(itemToRemove.ComputerGuid, out removed);
      if (wasRemoved)
      {
        new ElectionModel().UpdateElectionWhenComputerFreshnessChanges(AllForThisElection);
      }
    }

    /// <summary>
    ///   Update the LastContact in the cached computer
    /// </summary>
    public void UpdateLastContactOfCurrentComputer()
    {
      var computer = UserSession.CurrentComputer;
      CachedDict.AddOrUpdate(computer.ComputerGuid, computer, (i, existingComputer) =>
      {
        existingComputer.LastContact = DateTime.Now;
        return existingComputer;
      });
      new ElectionModel().UpdateElectionWhenComputerFreshnessChanges(AllForThisElection);
    }

    /// <summary>
    ///   Update the Location in the cached computer
    /// </summary>
    /// <param name="computer"></param>
    public void UpdateComputerLocation(Computer computer)
    {
      CachedDict.AddOrUpdate(computer.ComputerGuid, computer, (i, existingComputer) =>
      {
        existingComputer.LocationGuid = computer.LocationGuid;
        return existingComputer;
      });
    }

    /// <summary>
    ///   update the Teller info in the cached computer
    /// </summary>
    /// <param name="computer"></param>
    public void UpdateTellers(Computer computer)
    {
      CachedDict.AddOrUpdate(computer.ComputerGuid, computer, (i, existingComputer) =>
      {
        existingComputer.Teller1 = computer.Teller1;
        existingComputer.Teller2 = computer.Teller2;
        return existingComputer;
      });
    }
  }
}