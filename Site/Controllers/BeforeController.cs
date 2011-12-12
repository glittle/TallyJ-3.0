using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Models;

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
			return View(new PeopleModel());
		}

    public JsonResult RegisterVote(int id, string type, int last)
    {
      return new PeopleModel().RegisterVoteJson(id, type, last);
    }

	}
}