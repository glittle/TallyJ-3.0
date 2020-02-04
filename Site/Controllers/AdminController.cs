using System.Web.Mvc;
using TallyJ.Code;

namespace TallyJ.Controllers
{
  [ForAuthenticatedTeller]
  public class AdminController : BaseController
  {
    public ActionResult Index()
    {
      return View();
    }

  }
}