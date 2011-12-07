using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;
using TallyJ.Models;

namespace TallyJ.Controllers
{
  public class SetupController : BaseController
  {
    
    public ActionResult Index()
    {
      return View("Setup", new SetupModel());
    }

    public ActionResult People()
    {
      return View(new SetupModel());
    }

    public JsonResult SaveElection(Election election)
    {
      return new ElectionModel().SaveElection(election);
    }

    public JsonResult DetermineRules(string type, string mode)
    {
      return new ElectionModel().GetRules(type, mode).AsJsonResult();
    }

    [ForAuthenticatedTeller]
    public ActionResult ImportExport()
    {
      return View(new ImportExportModel());
    }

    public JsonResult SavePerson(Person person)
    {
      return new PeopleModel().SavePerson(person);
    }

    [ForAuthenticatedTeller]
    public JsonResult EditLocation(int id, string text)
    {
      return new LocationModel().EditLocation(id, text);
    }

    [ForAuthenticatedTeller]
    public JsonResult SortLocations(string ids)
    {
      return new LocationModel().SortLocations(ids);
    }

    [ForAuthenticatedTeller]
    public JsonResult ResetAll()
    {
      new PeopleModel().CleanAllPersonRecordsBeforeStarting();
      return "Done".AsJsonResult();
    }
  }
}