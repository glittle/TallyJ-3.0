using System;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.ExportImport;

namespace TallyJ.Controllers
{
  public class ElectionsController : BaseController
  {
    public ActionResult Index()
    {
      return null;
    }

    [ForAuthenticatedTeller]
    public JsonResult SelectElection(Guid guid)
    {
      var electionModel = new ElectionModel();

      if (electionModel.JoinIntoElection(guid))
      {
        return new
                 {
                   Locations = ContextItems.LocationModel.Locations.OrderBy(l => l.SortOrder).Select(l => new { l.Name, l.C_RowId }),
                   Selected = true,
                   ElectionName = UserSession.CurrentElectionName,
                   Pulse = new PulseModel(this).Pulse()
                 }.AsJsonResult();
      }
      return new {Selected = false}.AsJsonResult();
    }

    [ForAuthenticatedTeller]
    public JsonResult CopyElection(Guid guid)
    {
      var model = new ElectionModel();
      return model.Copy(guid);
    }

    [ForAuthenticatedTeller]
    public JsonResult UpdateElectionStatus(string status)
    {
      return new ElectionModel().SetTallyStatusJson(this, status);
    }

    [ForAuthenticatedTeller]
    public JsonResult CreateElection()
    {
      var model = new ElectionModel();
      return model.Create();
    }

    [ForAuthenticatedTeller]
    public ActionResult ExportElection(Guid guid)
    {
      var model = new ElectionExporter(guid);
      return model.Export();
    }

    [ForAuthenticatedTeller]
    public ActionResult DeleteElection(Guid guid)
    {
      var model = new ElectionDeleter(guid);
      return model.Delete();
    }
  }
}