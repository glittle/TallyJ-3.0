using System.Web.Mvc;

namespace TallyJ.Controllers
{
	public class BeforeController : Controller
	{
		//
		public ActionResult Index()
		{
			return View("Before");
		}

		public ActionResult FrontDesk()
		{
			return View();
		}
	}
}