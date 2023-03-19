using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  [AllowTellersInActiveElection]
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

    //[ForAuthenticatedTeller]
    public ActionResult Reports()
    {
      return View("Reports");
    }

    public ActionResult Presenter()
    {
      return View();
    }

    public ActionResult ShowTies()
    {
      return View(new ResultsModel());
    }

    public JsonResult GetTies(int tieBreakGroup)
    {
      return new ResultsModel().GetTies(tieBreakGroup).AsJsonResult();
    }

    public ActionResult Monitor()
    {
      return View(new MonitorModel());
    }

    public JsonResult RefreshMonitor()
    {
      new ComputerModel().RefreshLastContact();

      return new MonitorModel().MonitorInfo.AsJsonResult();
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
      return new ElectionHelper().UpdateElectionShowAllJson(showAll);
    }

    [ForAuthenticatedTeller]
    public JsonResult UpdateListing(bool listOnPage)
    {
      return new ElectionHelper().UpdateListOnPageJson(listOnPage);
    }

    //    [ForAuthenticatedTeller]
    //    public JsonResult CloseOnline(int minutes, bool est)
    //    {
    //      return new ElectionModel().CloseOnline(minutes, est);
    //    }

    [ForAuthenticatedTeller]
    public JsonResult SaveOnlineClose(DateTime when, bool est)
    {
      return new ElectionHelper().SaveOnlineClose(when, est);
    }

    [ForAuthenticatedTeller]
    public JsonResult ProcessOnlineBallots()
    {
      return new ElectionHelper().ProcessOnlineBallots();
    }

    //[ForAuthenticatedTeller]
    public JsonResult GetReportData(string code)
    {
      return new ResultsModel().GetReportData(code);
    }

    [ForAuthenticatedTeller]
    public JsonResult SaveTieCounts(List<string> counts)
    {
      return new ResultsModel().SaveTieCounts(counts);
    }

    [ForAuthenticatedTeller]
    public JsonResult SaveManual(ResultSummary manualResults)
    {
      return new ResultsModel().SaveManualResults(manualResults);
    }
  }
}