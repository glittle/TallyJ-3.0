using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;

namespace TallyJ.Models
{
  public class PulseModel : DataConnectedModel
  {
    private readonly UrlHelper _urlHelper;
    private readonly PulseInfo _infoFromClient;

    public PulseModel(Controller controller, PulseInfo infoFromClient = null)
    {
      _urlHelper = controller.Url;
      _infoFromClient = infoFromClient;
    }
    public PulseModel(UrlHelper urlHelper, PulseInfo infoFromClient = null)
    {
      _urlHelper = urlHelper;
      _infoFromClient = infoFromClient;
    }

    // properties expected:
    //  Active  -- is this computer active in an election?
    //  PulseSeconds -- seconds to delay before next pulse
    //  VersionNum -- last version in db

    public JsonResult ProcessPulseJson()
    {
      var result = Pulse();

      return result.AsJsonResult(JsonRequestBehavior.AllowGet);
    }

    public object Pulse()
    {
      var isStillAllowed = new ComputerModel().ProcessPulse();

      new ElectionModel().ProcessPulse();
      var statusChanged = false;

      if (_infoFromClient != null)
      {
        statusChanged = _infoFromClient.Status != UserSession.CurrentElectionStatus;
      }

      var newStatus = statusChanged
                              ? new
                              {
                                QuickLinks = new MenuHelper(_urlHelper).QuickLinks(),
                                Name = UserSession.CurrentElectionStatusName,
                                Code = UserSession.CurrentElectionStatus,
                              }
                              : null;

      var result = new
                       {
                         Active = isStillAllowed,
                         //VersionNum = new Random().Next(1, 100),
                         NewStatus = newStatus,
                         PulseSeconds = isStillAllowed ? 0 : 60 // if 0, client will use its current pulse number
                       };
      return result;
    }
  }
}