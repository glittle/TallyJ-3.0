using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.Models;

namespace TallyJ.Controllers
{
  public class DashboardController : BaseController
  {
    public ActionResult Index()
    {
      if (UserSession.CurrentElection == null || UserSession.CurrentLocation == null)
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

    public JsonResult ChooseLocation(int id)
    {
      return new {Selected = new ComputerModel().AddCurrentComputerIntoLocation(id)}.AsJsonResult();
    }


    public JsonResult ChooseTeller(int num, int teller, string newName = "")
    {
      return new TellerModel().ChooseTeller(num, teller, newName).AsJsonResult();
    }

    public JsonResult DeleteTeller(int id)
    {
      return new TellerModel().DeleteTeller(id).AsJsonResult();
    }
  }
}