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
          ViewBag.Message = "Your quintessential app description page.";

          return View();
        }

        public ActionResult Contact()
        {
          ViewBag.Message = "Your quintessential contact page.";

          return View();
        }

        public ActionResult Learning()
        {
          return View();
        }

        public JsonResult Heartbeat()
        {
          return new PulseModel().ProcessPulse();
        }

      public ActionResult Install()
      {
        return View();
      }
    }
}
