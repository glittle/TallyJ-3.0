using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code.Session;
using TallyJ.Models;
using TallyJ.Code;

namespace TallyJ.Controllers
{
  public class DashboardController : BaseController
  {
    public ActionResult Index()
    {
      if (UserSession.CurrentElection == null || UserSession.CurrentLocation == null)
      {
        return RedirectToAction("ChooseElection");
        // return View("ChooseElection", new ElectionsListViewModel());
      }

      return View(new ElectionsListViewModel());
    }

    public ActionResult ChooseElection()
    {
      return View(new ElectionsListViewModel());
    }

    public JsonResult ChooseLocation(int id)
    {
      return new { Selected = new ComputerModel().AddCurrentComputerIntoLocation(id) }.AsJsonResult();
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
