using System.Collections.Concurrent;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Hubs;
using TallyJ.Code.UnityRelated;
using TallyJ.Code.Data;
using System;

namespace TallyJ.EF
{
  //public class PublicElectionInfo
  //{
  //  public string Name { get; set; }
  //  public string Passcode { get; set; }
  //}

  public class PublicElectionLister
  {
    /// This static cache is shared across all elections in active use!
    //private static readonly ConcurrentDictionary<int, PublicElectionInfo> ElectionsCurrentlyOpenToGuestTellers = new ConcurrentDictionary<int, PublicElectionInfo>();
    private ITallyJDbContext _db;

    //public void UpdateThisElectionInList()
    //{
    //  var election = UserSession.CurrentElection;
    //  if (election == null)
    //  {
    //    return;
    //  }
    //  CheckElection(election);
    //}

    //private void CheckElection(Election election)
    //{
    //  var canBeAvailable = election.CanBeAvailableForGuestTellers;
    //  var electionId = election.C_RowId;

    //  var isAvailable = ElectionsCurrentlyOpenToGuestTellers.ContainsKey(electionId);
    //  if (!canBeAvailable)
    //  {
    //    new MainHub(). DisconnectGuests();
    //  }

    //  if (canBeAvailable == isAvailable)
    //  {
    //    return;
    //  }

    //  // something changed!
    //  if (!canBeAvailable)
    //  {
    //    PublicElectionInfo removedName;
    //    var wasRemoved = ElectionsCurrentlyOpenToGuestTellers.TryRemove(electionId, out removedName);
    //  }
    //  else
    //  {
    //    ElectionsCurrentlyOpenToGuestTellers[electionId] = new PublicElectionInfo
    //    {
    //      Name = election.Name + election.Convenor.SurroundContentWith(" (", ")"),
    //      Passcode = election.ElectionPasscode
    //    };
    //  }

    //  // the public listing changed
    //  new PublicHub().TellPublicAboutVisibleElections();
    //}

    /// <summary>
    /// Is this election Id in the list of publically visible ids?
    /// </summary>
    /// <param name="electionGuid"></param>
    /// <returns></returns>
    public string GetPasscodeIfAvailable(Guid electionGuid)
    {
      var activeElectionGuids = new ComputerCacher().ElectionGuidsOfActiveComputers.Where(g => g == electionGuid).ToList();
      if (activeElectionGuids.Count == 0)
      {
        return null;
      }

      var election = Db.Election
        .FirstOrDefault(e => e.ElectionGuid == electionGuid
                             && e.ListForPublic.HasValue
                             && e.ListForPublic.Value);
      return election == null ? null : election.ElectionPasscode;
    }

    protected ITallyJDbContext Db
    {
      get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
      set { _db = value; }
    }

    /// <summary>
    /// Refresh the list and return it.
    /// </summary>
    /// <returns></returns>
    public string RefreshAndGetListOfAvailableElections()
    {
      const string template = "<option value=\"{0}\">{1} {2}</option>";

      var activeElectionGuids = new ComputerCacher().ElectionGuidsOfActiveComputers;

      if (activeElectionGuids.Count == 0)
      {
        return template.FilledWith(0, "(No elections are active right now.)", "");
      }

      //TODO - does this hit the DB every time??
      var elections = Db.Election
        .Where(e => activeElectionGuids.Contains(e.ElectionGuid)
             && e.ListForPublic.HasValue
             && e.ListForPublic.Value
             && e.ElectionPasscode != null)
        .Select(e => new { e.Name, e.ElectionGuid, e.Convenor })
        .ToList();

      return elections
        .OrderBy(e => e.Name)
        .Select(e => template.FilledWith(e.ElectionGuid, e.Name, e.Convenor.SurroundContentWith("(", ")")))
        .JoinedAsString();
    }

  }
}