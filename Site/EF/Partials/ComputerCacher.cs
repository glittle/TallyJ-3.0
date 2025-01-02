using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Hubs;

namespace TallyJ.EF
{
  public class ComputerCacher // not extending the base cacher
  {
    /// This static cache is shared across all elections in active use!
    private static readonly ConcurrentDictionary<Guid, Computer> CachedDict = new();

    public List<Computer> AllForThisElection
    {
      get { return AllForElection(UserSession.CurrentElectionGuid); }
    }

    public List<Computer> AllForElection(Guid electionGuid)
    {
      return CachedDict.Values.Where(c => c.ElectionGuid == electionGuid).ToList();
    }

    /// <summary>
    ///   Add this new computer to the cache
    /// </summary>
    /// <param name="computer"></param>
    //public void AddToCache(Computer computer)
    //{
    //  var wasAdded = CachedDict.TryAdd(computer.ComputerGuid, computer);
    //  AssertAtRuntime.That(wasAdded, "can't add!");
    //}

    /// <summary>
    /// List of Guids of all Elections that have Known tellers
    /// </summary>
    public List<Guid> ElectionGuidsOfActiveComputers
    { 
      get
      {
        var maxAge = new TimeSpan(1, 0, 0); // 1 hour
        var utcNow = DateTime.UtcNow;

        var activeComputers = CachedDict.Values
          .Where(comp => (comp.AuthLevel == "Known" || comp.AuthLevel == "Left")
                         && comp.LastContact.HasValue 
                         && utcNow - comp.LastContact.Value.AsUtc() < maxAge)
          .ToList();

        return activeComputers
          .SelectMany(comp => comp.AllMyElections)
          .Distinct()
          .ToList();
      }
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
        //new ElectionModel().UpdateElectionWhenComputerFreshnessChanges(AllForThisElection);
        new PublicHub().TellPublicAboutVisibleElections();
      }
    }

    /// <summary>
    ///   Update the LastContact in the cached computer
    /// </summary>
    public void UpdateLastContactOfCurrentComputer()
    {
      var computer = UserSession.CurrentComputer;
      if (computer == null)
      {
        return;
      }

      computer.LastContact = DateTime.UtcNow;

      CachedDict.AddOrUpdate(computer.ComputerGuid, computer, (i, existingComputer) => computer);

      //if (computer.AuthLevel == "Known")
      //{
      //  // new ElectionModel().UpdateElectionWhenComputerFreshnessChanges(AllForThisElection);
      //  new PublicHub().TellPublicAboutVisibleElections();
      //}
    }

    /// <summary>
    ///   update the Teller info in the cached computer
    /// </summary>
    /// <param name="computer"></param>
    //public void UpdateTellers(Computer computer)
    //{
    //  CachedDict.AddOrUpdate(computer.ComputerGuid, computer, (i, existingComputer) =>
    //  {
    //    existingComputer.Teller1 = computer.Teller1;
    //    existingComputer.Teller2 = computer.Teller2;
    //    return existingComputer;
    //  });
    //}

    internal void UpdateComputer(Computer computer)
    {
      CachedDict.AddOrUpdate(computer.ComputerGuid, computer, (i, existingComputer) =>
      {
        return computer;
      });
    }
  }
}