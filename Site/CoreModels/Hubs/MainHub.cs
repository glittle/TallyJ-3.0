﻿using System;
using System.Linq;
using Microsoft.AspNet.SignalR;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;

namespace TallyJ.CoreModels.Hubs
{
  public class MainHub
  {
    private IHubContext _coreHub;

    private string HubNameForCurrentElection
    {
      get
      {
        var electionGuid = UserSession.CurrentElectionGuid;
        AssertAtRuntime.That(electionGuid != Guid.Empty);

        return "Main" +  electionGuid;
      }
    }

    private IHubContext CoreHub
    {
      get { return _coreHub ?? (_coreHub = GlobalHost.ConnectionManager.GetHubContext<MainHubCore>()); }
    }

    /// <summary>
    ///   Join this connection into the hub
    /// </summary>
    /// <param name="connectionId"></param>
    public void Join(string connectionId)
    {
      var group = HubNameForCurrentElection + (UserSession.IsKnownTeller ? "Known" : "Guest");

      CoreHub.Groups.Add(connectionId, group);

      new ComputerModel().RefreshLastContact();
    }

    /// <summary>
    ///   Join this connection into the hub
    /// </summary>
    /// <param name="connectionId"></param>
    /// <param name="electionGuidList">Comma sep list of guids</param>
    public void JoinAll(string connectionId, string electionGuidList)
    {
      if (!UserSession.IsKnownTeller)
      {
        return;
      }

      var guids = electionGuidList.SplitWithString(",").Select(s => s.AsGuid()).ToList();
      foreach (var guid in guids)
      {
        var group = $"Main{guid}Known";

        CoreHub.Groups.Add(connectionId, group);
      }

      new ComputerModel().RefreshLastContact();
    }

    public void StatusChanged(object infoForKnown, object infoForGuest)
    {
      CoreHub.Clients.Group(HubNameForCurrentElection + "Known").statusChanged(infoForKnown);
      CoreHub.Clients.Group(HubNameForCurrentElection + "Guest").statusChanged(infoForGuest);
    }

    public void StatusChangedForElection(Guid electionGuid,  object infoForKnown, object infoForGuest)
    {
      var hubName = "Main" + electionGuid;
      CoreHub.Clients.Group(hubName + "Known").statusChanged(infoForKnown);
      CoreHub.Clients.Group(hubName + "Guest").statusChanged(infoForGuest);
    }

    public void CloseOutGuestTellers()
    {
      CoreHub.Clients.Group(HubNameForCurrentElection + "Guest").electionClosed();
    }
    public void CloseOutGuestTellers(Guid electionGuid)
    {
      CoreHub.Clients.Group($"Main{electionGuid}Guest").electionClosed();
    }
  }

  public class MainHubCore : Hub
  {
    // empty class needed for signalR use!!
    // referenced by helper and in JavaScript
  }
}