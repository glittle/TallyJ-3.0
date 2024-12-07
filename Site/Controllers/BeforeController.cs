using System;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Hubs;

namespace TallyJ.Controllers
{
  [AllowTellersInActiveElection]
  public class BeforeController : BaseController
  {
    public ActionResult Index()
    {
      return null;
    }

    public ActionResult FrontDesk()
    {
      // ensure we are at a physical location!
      var currentLocation = UserSession.CurrentLocation;
      if (currentLocation == null || currentLocation.IsVirtual)
      {
        var newLocation = new LocationModel().GetLocations_Physical().First();
        UserSession.CurrentLocationGuid = newLocation.LocationGuid;
      }

      return View(new PeopleModel());
    }

    public ActionResult RollCall()
    {
      return View(new RollCallModel());
    }

    public JsonResult PeopleForFrontDesk(string unit = null) {
      if (unit.HasContent())
      {
        var currentElection = UserSession.CurrentElection;
        if (currentElection.ElectionType == ElectionTypeEnum.LSA2M.Value)
        {
          // temporary for this request
          currentElection.UnitName = unit;
        }
      }

      return new PeopleModel().FrontDeskPersonLines().AsJsonResult();
    }

    public JsonResult VotingMethod(int id, string type, int loc = 0, bool forceDeselect = false)
    {
      return new PeopleModel().RegisterVotingMethod(id, type, forceDeselect, loc);
    }

    public JsonResult SetFlag(int id, string type, int loc = 0, bool forceDeselect = false)
    {
      return new PeopleModel().SetFlag(id, type, forceDeselect, loc);
    }

    public void JoinFrontDeskHub(string connId)
    {
      new FrontDeskHub().Join(connId);
    }

    public void JoinRollCallHub(string connId)
    {
      new RollCallHub().Join(connId);
    }
  }
}