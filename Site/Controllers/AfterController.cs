using System.Web.Mvc;

namespace TallyJ.Controllers
{
  public class AfterController : Controller
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