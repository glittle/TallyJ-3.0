using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.ExportImport;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  public class DashboardController : BaseController
  {
    [AllowTellersInActiveElection]
    public ActionResult Index()
    {
      if (UserSession.CurrentElectionGuid == Guid.Empty || UserSession.CurrentElection == null)
      {
        // no current election
        return UserSession.IsKnownTeller
                 ? RedirectToAction("ElectionList")
                 : RedirectToAction("LogOff", "Account");
      }

      return View(new ElectionsListViewModel());
    }

    [ForAuthenticatedTeller]
    public ActionResult ElectionList()
    {
      if (UserSession.CurrentElectionGuid == Guid.Empty)
      {
        var currentComputer = UserSession.CurrentComputer;
        if (currentComputer == null)
        {
          new ComputerModel().GetTempComputerForMe();
        }
        else
        {
          new ComputerCacher().UpdateComputer(currentComputer);
        }
      }

      new PublicHub().TellPublicAboutVisibleElections();

      return View(new ElectionsListViewModel());
    }

    [ForAuthenticatedTeller]
    public JsonResult MoreInfoStatic()
    {
      return new ElectionsListViewModel().MoreInfoStatic().AsJsonResult();
    }
    [ForAuthenticatedTeller]
    public JsonResult MoreInfoLive()
    {
      return new ElectionsListViewModel().MoreInfoLive().AsJsonResult();
    }

    [ForAuthenticatedTeller]
    public JsonResult ReloadElection()
    {
      return new
      {
        Success = true,
        elections = new ElectionsListViewModel().MyElectionsInfo
      }.AsJsonResult();
    }

    [ForAuthenticatedTeller]
    public JsonResult UpdateListingForElection(bool listOnPage, Guid electionGuid)
    {
      // from the elections list page, when not "in" the election

      // verify we have access
      var election = new ElectionsListViewModel()
        .MyElections()
        .FirstOrDefault(e => e.ElectionGuid == electionGuid);

      if (election == null)
      {
        return new
        {
        Success = false,
          Message = "Unknown election"
        }.AsJsonResult();
      }

      // update
      if (UserSession.IsKnownTeller)
      {
        var electionCacher = new ElectionCacher(Db);

        Db.Election.Attach(election);

        election.ListForPublic = listOnPage;
        election.ListedForPublicAsOf = listOnPage ? (DateTime?)DateTime.UtcNow : null;

        Db.SaveChanges();

        electionCacher.UpdateItemAndSaveCache(election);

        new PublicHub().TellPublicAboutVisibleElections();

        if (!listOnPage)
        {
          new MainHub().CloseOutGuestTellers(electionGuid);
        }

        var info = new
        {
          ElectionGuid = electionGuid,
          StateName = election.TallyStatus.HasNoContent() ? ElectionTallyStatusEnum.NotStarted.ToString() : election.TallyStatus,
          Online = election.OnlineCurrentlyOpen,
          Passcode = election.ElectionPasscode,
          Listed = election.ListedForPublicAsOf != null
        };

        new MainHub().StatusChangedForElection(electionGuid, info, info);

        return new
        {
          Success = true,
          IsOpen = listOnPage
        }.AsJsonResult();
      }

      return new
      {
        Success = false
      }.AsJsonResult();
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