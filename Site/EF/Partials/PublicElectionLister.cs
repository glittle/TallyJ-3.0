using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EntityFramework.Caching;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Hubs;

namespace TallyJ.EF
{
  public class PublicElectionLister
  {
    /// This static cache is shared across all elections in active use!
    private static readonly ConcurrentDictionary<int, string> CachedDict = new ConcurrentDictionary<int, string>();

    public void UpdateThisElectionInList()
    {
      var election = UserSession.CurrentElection;
      if (election == null)
      {
        return;
      }
      var shouldBePublic = election.ListForPublicCalculated;
      var electionId = election.C_RowId;

      var isPublic = CachedDict.ContainsKey(electionId);
      if (shouldBePublic == isPublic)
      {
        return;
      }
      // something changed!
      if (!shouldBePublic)
      {
        string removedName;
        var wasRemoved = CachedDict.TryRemove(electionId, out removedName);
        new MainHub().DisconnectGuests();
      }
      else
      {
        CachedDict[electionId] = election.Name;
      }
      // the public listing changed
      new PublicHub().ElectionsListUpdated();
    }

    /// <summary>
    /// Is this election Id in the list of publically visible ids?
    /// </summary>
    /// <param name="electionId"></param>
    /// <returns></returns>
    public bool IsListed(int electionId)
    {
      return CachedDict.ContainsKey(electionId);
    }

    public string VisibleElectionsOptions()
    {
      const string template = "<option value=\"{0}\">{1}</option>";
      var listing = CachedDict.OrderBy(kvp=>kvp.Value).Select(kvp => template.FilledWith(kvp.Key, kvp.Value)).JoinedAsString();
      return listing
        .DefaultTo(template.FilledWith(0, "(Sorry, no elections are active right now.)"));
    }

  }
}