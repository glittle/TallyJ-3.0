using System;
using System.Web.Mvc;
using TallyJ.Code;
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
      return View(new PeopleModel());
    }

    public ActionResult RollCall()
    {
      return View(new RollCallModel());
    }

    public JsonResult PeopleForFrontDesk() {
      return new PeopleModel().FrontDeskPersonLines().AsJsonResult();
    }

    public JsonResult VotingMethod(int id, string type, int last, bool forceDeselect, int loc)
    {
      return new PeopleModel().RegisterVotingMethod(id, type, last, forceDeselect, loc);
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