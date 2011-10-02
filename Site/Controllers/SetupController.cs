using System.Web.Mvc;
using TallyJ.Code;
using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.Controllers
{
	public class SetupController : BaseController
	{
		//
		// GET: /Setup/

		public ActionResult Index()
		{
		  ContextItems.AddJavascriptForPage("setupIndexInfo", "setupIndexPage.info={0};".FilledWith(
		    new
		      {
		        Election = UserSession.CurrentElection
		      }.SerializedAsJson()));
			return View();
		}
	}
}