using System;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.ExportImport;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  [AllowTellersInActiveElection]
  public class ElectionsController : BaseController
  {
    public ActionResult Index()
    {
      return null;
    }

    [ForAuthenticatedTeller]
    public JsonResult SelectElection(Guid guid, Guid? oldComputerGuid)
    {
      var electionHelper = new ElectionHelper();

      if (electionHelper.JoinIntoElection(guid, oldComputerGuid.AsGuid()))
      {
        return new
                 {
                   Locations = ContextItems.LocationModel.GetLocations_Physical().OrderBy(l => l.SortOrder).Select(l => new { l.Name, l.C_RowId }),
                   Selected = true,
                   ElectionName = UserSession.CurrentElectionName,
                   ElectionGuid = UserSession.CurrentElectionGuid,
                   CompGuid = UserSession.CurrentComputer.ComputerGuid,
                   // Pulse = new PulseModel(this).Pulse()
                 }.AsJsonResult();
      }
      return new {Selected = false}.AsJsonResult();
    }

//    [ForAuthenticatedTeller]
//    public JsonResult CopyElection(Guid guid)
//    {
//      var model = new ElectionModel();
//      return model.Copy(guid);
//    }

    [ForAuthenticatedTeller]
    public JsonResult UpdateElectionStatus(string state)
    {
      return new ElectionHelper().SetTallyStatusJson(state);
    }

    [ForAuthenticatedTeller]
    public JsonResult CreateElection()
    {
      var model = new ElectionHelper();
      return model.Create();
    }

    [ForAuthenticatedTeller]
    public JsonResult JoinImportHub(string connId)
    {
      new ImportHub().Join(connId);
      return true.AsJsonResult();
    }


    [ForAuthenticatedTeller]
    public JsonResult JoinAnalyzeHub(string connId)
    {
      new AnalyzeHub().Join(connId);
      return true.AsJsonResult();
    }

    [ForAuthenticatedTeller]
    public ActionResult ExportElection(Guid guid)
    {
      var model = new ElectionExporter(guid);
      return model.Export();
    }

    [ForAuthenticatedTeller]
    public JsonResult ResetCache()
    {
      // wipe cached results - this wipes for everyone looking at this election
      
      new CacherHelper().DropAllCachesForThisElection();

      return "Cache cleared.".AsJsonResult(JsonRequestBehavior.AllowGet);
    }

    [ForAuthenticatedTeller]
    public ActionResult DeleteElection(Guid guid)
    {
      var model = new ElectionDeleter(guid);
      return model.Delete();
    }
  }
}