using Microsoft.AspNet.SignalR;
using TallyJ.EF;

namespace TallyJ.CoreModels.Hubs
{
  public class ImportHub
  {
    private IHubContext _coreHub;

    private string HubNameForPublic
    {
      get { return "Import"; }
    }

    private IHubContext CoreHub
    {
      get { return _coreHub ?? (_coreHub = GlobalHost.ConnectionManager.GetHubContext<ImportHubCore>()); }
    }

    /// <summary>
    ///   Join this connection into the hub
    /// </summary>
    /// <param name="connectionId"></param>
    public void Join(string connectionId)
    {
      CoreHub.Groups.Add(connectionId, HubNameForPublic);
    }

    public void ImportInfo(int linesProcessed, int peopleAdded)
    {
      CoreHub.Clients.Group(HubNameForPublic).ImportInfo(linesProcessed, peopleAdded);
    }
  }

  public class ImportHubCore : Hub
  {
    // empty class needed for signalR use!!
    // referenced by helper and in JavaScript
  }
}