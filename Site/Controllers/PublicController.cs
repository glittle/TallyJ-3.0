using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.Models;

namespace TallyJ.Controllers
{
  public class PublicController : Controller
  {
    //
    // GET: /Public/

    public ActionResult Index()
    {
      return View("Home", new PublicHomeViewModel());
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

    public JsonResult Heartbeat()
    {
      return new PulseModel().ProcessPulseJson();
    }

    public ActionResult Install()
    {
      return View();
    }

    public JsonResult TellerJoin(int election, string pc)
    {
      return new TellerModel().GrantAccessToGuestTeller(election, pc);
    }
  }
}
