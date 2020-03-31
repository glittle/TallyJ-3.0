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
        var emailOrPhone = UserSession.VoterId;
        AssertAtRuntime.That(emailOrPhone.HasContent());

        return "Voter" + emailOrPhone;
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
      if (person != null)
      {
        if (person.Email.HasContent())
        {
          CoreHub.Clients.Group("Voter" + person.Email).updateVoter(new
          {
            updateRegistration = true,
            person.VotingMethod,
            person.RegistrationTime,
            person.ElectionGuid
          });
        }
      
        if (person.Phone.HasContent())
        {
          CoreHub.Clients.Group("Voter" + person.Phone).updateVoter(new
          {
            updateRegistration = true,
            person.VotingMethod,
            person.RegistrationTime,
            person.ElectionGuid
          });
        }
      }
    }

    public void Login(string emailOrPhone)
    {
      CoreHub.Clients.Group("Voter" + emailOrPhone).updateVoter(new
      {
        login = true
      });
    }
  }


  public class VoterPersonalHubCore : Hub
  {
  }
}