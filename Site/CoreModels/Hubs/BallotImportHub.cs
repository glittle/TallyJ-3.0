using Microsoft.AspNet.SignalR;
using System;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels.Hubs
{
  public class BallotImportHub : IStatusUpdateHub
  {
    private IHubContext _coreHub;

    private string HubNameForPublic
    {
      get {
        return "Import" + UserSession.LoginId;
      }
    }

    private IHubContext CoreHub
    {
      get { return _coreHub ?? (_coreHub = GlobalHost.ConnectionManager.GetHubContext<BallotImportHubCore>()); }
    }

    /// <summary>
    ///   Join this connection into the hub
    /// </summary>
    /// <param name="connectionId"></param>
    public void Join(string connectionId)
    {
      CoreHub.Groups.Add(connectionId, HubNameForPublic);
    }

    public void StatusUpdate(string msg, bool msgIsTemp = false)
    {
      CoreHub.Clients.Group(HubNameForPublic).StatusUpdate(msg, msgIsTemp);
    }
  }

  public class BallotImportHubCore: Hub
  {
    // empty class needed for signalR use!!
    // referenced by helper and in JavaScript
  }
}