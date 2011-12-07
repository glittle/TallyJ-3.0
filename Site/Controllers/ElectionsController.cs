using System;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.Models;

namespace TallyJ.Controllers
{
  public class ElectionsController : BaseController
  {
    public ActionResult Index()
    {
      return null;
    }

    public JsonResult SelectLocation(int id)
    {
      return new { Selected = new ComputerModel().AddCurrentComputerIntoLocation(id) }.AsJsonResult();
    }

    [ForAuthenticatedTeller]
    public JsonResult SelectElection(Guid guid)
    {
      var electionModel = new ElectionModel();
      var locationModel = new LocationModel();

      if (electionModel.JoinIntoElection(guid))
      {
        return new
                 {
                   Locations = locationModel.LocationsForCurrentElection.OrderBy(l => l.SortOrder).Select(l => new {l.Name, l.C_RowId}),
                   Selected = true,
                   ElectionName = UserSession.CurrentElectionName,
                   Pulse = new PulseModel().Pulse()
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
    public JsonResult CreateElection()
    {
      var model = new ElectionModel();
      return model.Create();
    }
  }
}