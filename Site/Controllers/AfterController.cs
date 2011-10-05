using System.Web.Mvc;
using TallyJ.Code;

namespace TallyJ.Controllers
{
  public class AfterController : BaseController
  {
    //
    // GET: /Setup/

    public ActionResult Index()
    {
      return View("After");
    }

    public ActionResult Analyze()
    {
      return View();
    }

    public ActionResult Reports()
    {
      return View();
    }

    public ActionResult Presenter()
    {
      return View();
    }

    public ActionResult Monitor()
    {
      return View();
    }
  }
}