using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;

namespace TallyJ.CoreModels.Hubs
{
  public class VoterHub
  {
    private IHubContext _coreHub;

    private string HubNameForCurrentElection
    {
      get
      {
        var electionGuid = UserSession.CurrentElectionGuid;
        AssertAtRuntime.That(electionGuid != Guid.Empty);

        return "Voter" + electionGuid;
      }
    }

    private IHubContext CoreHub
    {
      get { return _coreHub ?? (_coreHub = GlobalHost.ConnectionManager.GetHubContext<VoterHubCore>()); }
    }

    /// <summary>
    /// Join this connection into the hub
    /// </summary>
    /// <param name="connectionId"></param>
    public void Join(string connectionId)
    {
      CoreHub.Groups.Add(connectionId, HubNameForCurrentElection);
    }

    public void UpdateElection(object message)
    {
      CoreHub.Clients.Group(HubNameForCurrentElection).updateElection(message);
    }

    //    public int NumAttached
    //    {
    //      get
    //      {
    //        var myHubName = GetType().Name + "Core";
    //        return !HubCounter.ConnectedIds.ContainsKey(myHubName) ? 0 : HubCounter.ConnectedIds[myHubName].Count;
    //      }
    //    }
  }

  public class VoterHubCore : Hub
  {
  }
}