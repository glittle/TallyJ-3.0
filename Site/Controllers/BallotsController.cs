using System.Web.Mvc;
using System.Web.Providers.Entities;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.Models;

namespace TallyJ.Controllers
{
	public class BallotsController : BaseController
	{
		//
		// GET: /Setup/

	  public ActionResult Index()
	  {
	    var model = new BallotModel();
      if (UserSession.CurrentElection.IsSingleNameElection.AsBool())
      {
        return View("BallotSingle", model);
      }

	    return View("Ballots", model);
		}
	}
}