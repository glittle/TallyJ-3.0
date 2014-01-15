using System.Collections.Generic;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;

namespace TallyJ.CoreModels
{
  public class PulseModel : DataConnectedModel
  {
    private readonly PulseInfo _infoFromClient;
    private readonly UrlHelper _urlHelper;

    public PulseModel(Controller controller, PulseInfo infoFromClient = null)
    {
      _urlHelper = controller.Url;
      _infoFromClient = infoFromClient;
    }

//    public PulseModel(UrlHelper urlHelper, PulseInfo infoFromClient = null)
//    {
//      _urlHelper = urlHelper;
//      _infoFromClient = infoFromClient;
//    }

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
      if (!UserSession.IsLoggedIn)
      {
        return false;
      }

      var result2 = new Dictionary<string, object>();

      var isStillAllowed = new ComputerModel().ProcessPulse();
      //new ElectionModel().ProcessPulse();

      var statusChanged = false;

      if (_infoFromClient != null)
      {
        statusChanged = _infoFromClient.Status != UserSession.CurrentElectionStatus;

//        switch (_infoFromClient.Context)
//        {
//          case "BeforeRollCall":
//            var rollcall = new RollCallModel();
//            long newStamp;
//            result2.Add("MorePeople", rollcall.GetMorePeople(_infoFromClient.Stamp, out newStamp));
//            result2.Add("NewStamp", newStamp);
//            break;
//        }
      }

      var newStatus = statusChanged
                          ? new
                              {
//                                QuickLinks = new MenuHelper(_urlHelper).QuickLinks(),
//                                Name = UserSession.CurrentElectionStatusName,
                                Code = UserSession.CurrentElectionStatus,
                              }
                          : null;

      if (newStatus != null)
      {
        result2.Add("NewStatus", newStatus);
      }
      result2.Add("Active", isStillAllowed);
      result2.Add("PulseSeconds", isStillAllowed ? 0 : 60); // if 0, client will use its current pulse number

      return result2;
    }
  }
}