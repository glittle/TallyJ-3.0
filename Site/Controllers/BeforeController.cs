using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Models;

namespace TallyJ.Controllers
{
	public class BeforeController : BaseController
	{
    public ActionResult Index()
    {
      return null;
    }

    public ActionResult FrontDesk()
		{
			return View(new PeopleModel());
		}

    public ActionResult RoleCall()
		{
			return View(new RoleCallModel());
		}

    public JsonResult RegisterVote(int id, string type, int last)
    {
      return new PeopleModel().RegisterVoteJson(id, type, last);
    }

	}
}