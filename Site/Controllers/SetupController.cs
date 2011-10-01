using System.Web.Mvc;
using TallyJ.Code;
using System.Linq;

namespace TallyJ.Controllers
{
	public class SetupController : BaseController
	{
		//
		// GET: /Setup/

		public ActionResult Index()
		{
			var election = DbContext.Elections.FirstOrDefault();
			return View(election);
		}
	}
}