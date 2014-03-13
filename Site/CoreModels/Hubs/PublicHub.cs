using Microsoft.AspNet.SignalR;
using TallyJ.EF;

namespace TallyJ.CoreModels.Hubs
{
  public class PublicHub
  {
    private IHubContext _coreHub;

    private string HubNameForPublic
    {
      get { return "Public"; }
    }

    private IHubContext CoreHub
    {
      get { return _coreHub ?? (_coreHub = GlobalHost.ConnectionManager.GetHubContext<PublicHubCore>()); }
    }

    /// <summary>
    ///   Join this connection into the hub
    /// </summary>
    /// <param name="connectionId"></param>
    public void Join(string connectionId)
    {
      CoreHub.Groups.Add(connectionId, HubNameForPublic);
    }

    public void ElectionsListUpdated()
    {
      CoreHub.Clients.Group(HubNameForPublic).ElectionsListUpdated(new PublicElectionLister().VisibleElectionsOptions());
    }
  }

  public class PublicHubCore : Hub
  {
    // empty class needed for signalR use!!
    // referenced by helper and in JavaScript
  }
}