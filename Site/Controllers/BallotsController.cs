using System.Web.Mvc;

namespace TallyJ.Controllers
{
	public class BallotsController : Controller
	{
		//
		// GET: /Setup/

		public ActionResult Index()
		{
			return View("Ballots");
		}

	}
}