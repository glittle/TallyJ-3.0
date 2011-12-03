using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Models;

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
      //var resultsModel = new ResultsModel();

      //resultsModel.GenerateResults();

      //return View(resultsModel);
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
      return View(new MonitorModel());
    }

    public JsonResult RefreshMonitor()
    {
      return new MonitorModel().LocationInfo.AsJsonResult();
    }

    public JsonResult RunAnalyze()
    {
      var resultsModel = new ResultsModel();

      resultsModel.GenerateResults();

      return resultsModel.CurrentResults;
    }
  }

}