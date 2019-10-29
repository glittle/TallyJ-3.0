using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.ExportImport;

namespace TallyJ.Controllers
{
  public class DashboardController : BaseController
  {
    [AllowTellersInActiveElection]
    public ActionResult Index()
    {
      if (UserSession.CurrentElectionGuid == Guid.Empty || UserSession.CurrentElection == null)
      {
        return UserSession.IsKnownTeller
                 ? RedirectToAction("ChooseElection")
                 : RedirectToAction("LogOff", "Account");
      }

      return View(new ElectionsListViewModel());
    }

    public ActionResult ChooseElection()
    {
      return View(new ElectionsListViewModel());
    }

    public JsonResult ElectionCounts() {
      return new ElectionsListViewModel().ElectionCounts().AsJsonResult();
    }


    [HttpPost]
    [AllowTellersInActiveElection]
    public JsonResult LoadV2Election(HttpPostedFileBase loadFile)
    {
      return new ElectionLoader().Import(loadFile);
    }

    [AllowTellersInActiveElection]
    public JsonResult ChooseLocation(int id)
    {
      return new { Selected = new ComputerModel().MoveCurrentComputerIntoLocation(id) }.AsJsonResult();
    }


    [AllowTellersInActiveElection]
    public JsonResult ChooseTeller(int num, int teller, string newName = "")
    {
      return new TellerModel().ChooseTeller(num, teller, newName).AsJsonResult();
    }

    [AllowTellersInActiveElection]
    public JsonResult DeleteTeller(int id)
    {
      return new TellerModel().DeleteTeller(id).AsJsonResult();
    }
  }
}