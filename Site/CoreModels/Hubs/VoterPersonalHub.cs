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
        var voterId = UserSession.VoterId;
        AssertAtRuntime.That(voterId.HasContent());

        return "Voter" + voterId;
      }
    }

    private IHubContext CoreHub => _coreHub
                                   ?? (_coreHub = GlobalHost.ConnectionManager.GetHubContext<VoterPersonalHubCore>());

    /// <summary>
    ///   Join this connection into the hub
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
        // 2022-02 how is this needed?
        if (person.Email.HasContent())
          CoreHub.Clients.Group("Voter" + person.Email).updateVoter(new
          {
            updateRegistration = true,
            person.VotingMethod,
            RegistrationTime = person.RegistrationTime.AsUtc(),
            person.ElectionGuid
          });

        if (person.Phone.HasContent())
          CoreHub.Clients.Group("Voter" + person.Phone).updateVoter(new
          {
            updateRegistration = true,
            person.VotingMethod,
            RegistrationTime = person.RegistrationTime.AsUtc(),
            person.ElectionGuid
          });
      }
    }

    public void Login(string voterId)
    {
      CoreHub.Clients.Group("Voter" + voterId).updateVoter(new
      {
        login = true
      });
    }
  }


  public class VoterPersonalHubCore : Hub
  {
  }
}