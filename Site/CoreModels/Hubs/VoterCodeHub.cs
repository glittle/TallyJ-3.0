using System;
using Microsoft.AspNet.SignalR;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels.Hubs
{
  public class VoterCodeHub
  {
    private IHubContext _coreHub;

    private IHubContext CoreHub => _coreHub ?? (_coreHub = GlobalHost.ConnectionManager.GetHubContext<VoterCodeHubCore>());

    public void SetStatus(string key, string message, string voiceCallStatusCode = null)
    {
      if (key.HasNoContent())
      {
        return;
      }
      CoreHub.Clients.Group(key).setStatus(message, voiceCallStatusCode);
    }

    public void Final(string key, bool okay, string message)
    {
      CoreHub.Clients.Group(key).final(okay, message);
    }

    /// <summary>
    /// Join this connection into the hub
    /// </summary>
    public void Join(string connectionId, string key)
    {
      CoreHub.Groups.Add(connectionId, key);
      // SetStatus(key, "B...");
    }
  }


  public class VoterCodeHubCore : Hub
  {
  }
}