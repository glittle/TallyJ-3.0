using Microsoft.AspNet.SignalR;
using System;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels.Hubs
{
  public class AnalyzeHub : IAnalyzeHub
  {
    private IHubContext _coreHub;

    private string HubNameForPublic
    {
      get {
        var electionGuid = UserSession.CurrentElectionGuid;
        AssertAtRuntime.That(electionGuid != Guid.Empty);

        return "Analyze" + electionGuid;
      }
    }

    private IHubContext CoreHub
    {
      get { return _coreHub ?? (_coreHub = GlobalHost.ConnectionManager.GetHubContext<AnalyzeHubCore>()); }
    }

    /// <summary>
    ///   Join this connection into the hub
    /// </summary>
    /// <param name="connectionId"></param>
    public void Join(string connectionId)
    {
      CoreHub.Groups.Add(connectionId, HubNameForPublic);
    }

    public void LoadStatus(string msg, bool msgIsTemp = false)
    {
      CoreHub.Clients.Group(HubNameForPublic).LoadStatus(msg, msgIsTemp);
    }
  }

  public class AnalyzeHubCore : Hub
  {
    // empty class needed for signalR use!!
    // referenced by helper and in JavaScript
  }

  public interface IAnalyzeHub {
    void LoadStatus(string msg, bool msgIsTemp = false);
  }
}