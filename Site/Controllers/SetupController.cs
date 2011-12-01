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

    public ActionResult ImportExport()
    {
      return View(new ImportExportModel());
    }

    public JsonResult SavePerson(Person person)
    {
      return new PeopleModel().SavePerson(person);
    }

    public JsonResult ResetAll()
    {
      new PeopleModel().CleanAllPersonRecordsBeforeStarting();
      return "Done".AsJsonResult();
    }
  }
}