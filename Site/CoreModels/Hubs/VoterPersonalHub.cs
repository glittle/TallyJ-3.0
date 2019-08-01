using System;
using Microsoft.AspNet.SignalR;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels.Hubs
{
  public class VoterPersonalHub
  {
    private IHubContext _coreHub;

    private string HubNameForCurrentVoter
    {
      get
      {
        var email = UserSession.VoterEmail;
        AssertAtRuntime.That(email.HasContent());

        return "Voter" + email;
      }
    }

    private IHubContext CoreHub
    {
      get { return _coreHub ?? (_coreHub = GlobalHost.ConnectionManager.GetHubContext<VoterPersonalHubCore>()); }
    }

    /// <summary>
    /// Join this connection into the hub
    /// </summary>
    /// <param name="connectionId"></param>
    public void Join(string connectionId)
    {
      CoreHub.Groups.Add(connectionId, HubNameForCurrentVoter);
    }

    public void Update(Person person)
    {
      if (person != null && person.Email.HasContent())
      {
        CoreHub.Clients.Group("Voter" + person.Email).updateVoter(new
        {
          person.VotingMethod,
          person.RegistrationTime,
          person.ElectionGuid
        });
      }
    }
  }


  public class VoterPersonalHubCore : Hub
  {
  }
}