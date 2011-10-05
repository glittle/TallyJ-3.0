using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code.Session;
using TallyJ.Models;
using TallyJ.Code;

namespace TallyJ.Controllers
{
	public class HomeController : BaseController
	{
		public ActionResult Index()
		{

			return View("Home", new HomeViewModel());
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

		public JsonResult Heartbeat(string computerCode, string teller1, string teller2)
		{
			var active = new Random().NextDouble() > 0.7;
			return new
			       	{
			       		Active = active,
                VersionNum = new Random().Next(1, 100)
			       	}.AsJsonResult();
		}
	}
}
