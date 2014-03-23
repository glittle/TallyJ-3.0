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
  public class PublicElectionInfo
  {
    public string Name { get; set; }
    public string Passcode { get; set; }
  }
  public class PublicElectionLister
  {
    /// This static cache is shared across all elections in active use!
    private static readonly ConcurrentDictionary<int, PublicElectionInfo> CachedDict = new ConcurrentDictionary<int, PublicElectionInfo>();

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
        PublicElectionInfo removedName;
        var wasRemoved = CachedDict.TryRemove(electionId, out removedName);
        new MainHub().DisconnectGuests();
      }
      else
      {
        CachedDict[electionId] = new PublicElectionInfo { Name = election.Name + election.Convenor.SurroundContentWith(" (", ")"), Passcode = election.ElectionPasscode };
      }
      // the public listing changed
      new PublicHub().ElectionsListUpdated();
    }

    /// <summary>
    /// Is this election Id in the list of publically visible ids?
    /// </summary>
    /// <param name="electionId"></param>
    /// <returns></returns>
    public PublicElectionInfo PublicElectionInfo(int electionId)
    {
      return CachedDict.ContainsKey(electionId) ? CachedDict[electionId] : null;
    }

    public string VisibleElectionsOptions()
    {
      const string template = "<option value=\"{0}\">{1}</option>";
      var listing = CachedDict.OrderBy(kvp => kvp.Value.Name).Select(kvp => template.FilledWith(kvp.Key, kvp.Value.Name)).JoinedAsString();
      return listing
        .DefaultTo(template.FilledWith(0, "(Sorry, no elections are active right now.)"));
    }

  }
}