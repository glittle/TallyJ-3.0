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

    public JsonResult SelectElection(Guid guid)
    {
      var model = new ElectionModel();

      if (model.JoinIntoElection(guid))
      {
        return new
                 {
                   Locations = model.LocationsForCurrentElection.OrderBy(l => l.SortOrder).Select(l => new {l.Name, l.C_RowId}),
                   Selected = true,
                   ElectionName = UserSession.CurrentElectionName,
                   Pulse = new PulseModel().Pulse()
                 }.AsJsonResult();
      }
      return new {Selected = false}.AsJsonResult();
    }

    public JsonResult CopyElection(Guid guid)
    {
      var model = new ElectionModel();
      return model.Copy(guid);
    }

    public JsonResult CreateElection()
    {
      var model = new ElectionModel();
      return model.Create();
    }
  }
}