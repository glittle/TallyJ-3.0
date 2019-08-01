using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;

namespace TallyJ.CoreModels.Hubs
{
  public class AllVotersHub
  {
    private IHubContext _coreHub;

    private string HubName
    {
      get
      {
        return "AllVoters";
      }
    }

    private IHubContext CoreHub
    {
      get { return _coreHub ?? (_coreHub = GlobalHost.ConnectionManager.GetHubContext<AllVotersHubCore>()); }
    }

    /// <summary>
    /// Join this connection into the hub
    /// </summary>
    /// <param name="connectionId"></param>
    public void Join(string connectionId)
    {
      CoreHub.Groups.Add(connectionId, HubName);
    }

    public void UpdateVoters(object message)
    {
      CoreHub.Clients.Group(HubName).updateVoters(message);
    }
  }

  public class AllVotersHubCore : Hub
  {
  }
}