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
	    var x = UserSession.IsLoggedIn;

	    var model = new BallotModel();
      if (UserSession.CurrentElection.IsSingleNameElection.AsBool())
      {
        return View("BallotSingle", model);
      }

	    return View("Ballots", model);
		}

    public JsonResult SaveVoteSingle(int pid, int vid, int count)
    {
      return new BallotModel().SaveVoteSingle(pid, vid, count);
    }
	}
}