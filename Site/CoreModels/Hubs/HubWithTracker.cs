using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace TallyJ.CoreModels.Hubs
{

  public abstract class HubWithTracker : Hub
  {
//    public static HashSet<string> ActiveIds = new HashSet<string>();
//
//    // empty class needed for signalR use!!
//    // referenced by helper and in JavaScript
//    public override Task OnConnected()
//    {
//      ActiveIds.Add(Context.ConnectionId);
//      return base.OnConnected();
//    }
//
//    public override Task OnDisconnected()
//    {
//      ActiveIds.Remove(Context.ConnectionId);
//      return base.OnDisconnected();
//    }
  }
}