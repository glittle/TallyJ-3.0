using System.Web.Mvc;
using TallyJ.Code;

namespace TallyJ.Controllers
{
	public class BeforeController : BaseController
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