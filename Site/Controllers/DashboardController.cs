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
        return View("ChooseElection", new ElectionsListViewModel());
      }

      return View(new ElectionsListViewModel());
    }

    public ActionResult ChooseElection()
    {
      return View(new ElectionsListViewModel());
    }
  }
}
