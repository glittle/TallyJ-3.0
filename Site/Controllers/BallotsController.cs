using System.Web.Mvc;
using TallyJ.Code;

namespace TallyJ.Controllers
{
	public class BallotsController : BaseController
	{
		//
		// GET: /Setup/

	  public ActionResult Index()
		{
			return View("Ballots");
		}
	}
}