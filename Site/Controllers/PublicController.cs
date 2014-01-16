using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Hubs;

namespace TallyJ.Controllers
{
  public class PublicController : Controller
  {
    //
    // GET: /Public/

    public ActionResult Index()
    {
      return View("Home");
    }

    public ActionResult About()
    {
      return View();
    }

    public ActionResult Contact()
    {
      return View();
    }

    public ActionResult Learning()
    {
      return View();
    }

    public JsonResult Heartbeat(PulseInfo info)
    {
      return new PulseModel(this, info).ProcessPulseJson();
    }

    public ActionResult Install()
    {
      return View();
    }

    public JsonResult TellerJoin(int election, string pc)
    {
      return new TellerModel().GrantAccessToGuestTeller(election, pc);
    }

    public JsonResult GetTimeOffset(long now, string tz)
    {
      // adjust client time by .5 seconds to allow for network and server time
      const double fudgeFactor = .5 * 1000;
      var clientTimeNow = new DateTime(1970, 1, 1).AddMilliseconds(now + fudgeFactor);
      var serverTime = DateTime.Now;
      var diff = (serverTime - clientTimeNow).TotalMilliseconds;
      UserSession.TimeOffsetServerAhead = diff.AsInt();
      UserSession.TimeOffsetKnown = true;
      return new
               {
                 timeOffset = diff
               }.AsJsonResult();
    }


    public JsonResult OpenElections()
    {
      return new
               {
                 html = new ElectionsListViewModel().VisibleElectionsOptions()
               }.AsJsonResult();
    }

    public JsonResult PublicHub(string connId)
    {
      new PublicHub().Join(connId);
      return OpenElections();
    }

    [Authorize]
    public void MainHub(string connId, string electionGuid)
    {
      // likely redundant - can remove when settled
      AssertAtRuntime.That(UserSession.CurrentElectionGuid != Guid.Empty, "not logged in");
      AssertAtRuntime.That(electionGuid.AsGuid() != Guid.Empty, "unknown election");

      new MainHub().Join(connId);
    }

  }

}
