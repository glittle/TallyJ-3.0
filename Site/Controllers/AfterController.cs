using System.Collections.Generic;
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

    [ForAuthenticatedTeller]
    public ActionResult Analyze()
    {
      var resultsModel = new ResultsModel();

      return View(resultsModel);
    }

    [ForAuthenticatedTeller]
    public ActionResult Reports()
    {
      return View("Reports");
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

    [ForAuthenticatedTeller]
    public JsonResult RunAnalyze()
    {
      var resultsModel = new ResultsModel();

      resultsModel.GenerateResults();

      return resultsModel.GetCurrentResults().AsJsonResult();
    }

    public JsonResult GetReport()
    {
      var resultsModel = new ResultsModel();

      return resultsModel.FinalResultsJson;
    }

    [ForAuthenticatedTeller]
    public JsonResult UpdateElectionShowAll(bool showAll)
    {
      return new ElectionModel().UpdateElectionShowAllJson(showAll);
    }
    [ForAuthenticatedTeller]
    public JsonResult UpdateListing(bool listOnPage)
    {
      return new ElectionModel().UpdateListOnPageJson(listOnPage);
    }
    [ForAuthenticatedTeller]
    public JsonResult GetReportData(string code)
    {
      return new ResultsModel().GetReportData(code);
    }
    [ForAuthenticatedTeller]
    public JsonResult SaveTieCounts(List<string> counts)
    {
      return new ResultsModel().SaveTieCounts(counts);
    }

    public ActionResult Pdf(string id)
    {
      var path = HttpContext.Server.MapPath(string.Format("~/Reports/{0}.rpt", id));
      if (System.IO.File.Exists(path))
      {
        var model = new CrystalReportsModel(path);
        return model.PdfResult;
      }
      ViewBag.Error = "Invalid report name";
      return View("ReportsCR");
    }
  }

}