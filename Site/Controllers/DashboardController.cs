using System;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.ExportImport;
using TallyJ.CoreModels.Helper;
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
    public JsonResult ReloadElections()
    {
      return new
      {
        Success = true,
        elections = new ElectionsListViewModel().GetMyElectionsInfo(true)
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
        election.ListedForPublicAsOf = listOnPage ? DateTime.UtcNow : null;

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

    [ForAuthenticatedTeller]
    public JsonResult RemoveFullTeller(string email, int joinId)
    {
      var join = Db.JoinElectionUser
        .FirstOrDefault(je => je.C_RowId == joinId);

      if (join == null)
      {
        return new
        {
          Success = false,
          Message = "Unknown id"
        }.AsJsonResult();
      }

      var targetElectionGuid = join.ElectionGuid;

      // ensure that I am the owner of the election
      var myJoin = Db.JoinElectionUser
        .FirstOrDefault(je => je.ElectionGuid == targetElectionGuid && je.UserId == UserSession.UserGuid);
      if (myJoin == null || myJoin.Role != null)
      {
        return new
        {
          Success = false,
          Message = "Removal not allowed"
        }.AsJsonResult();
      }

      Db.JoinElectionUser.Remove(join);

      Db.SaveChanges();

      if (email.HasContent() || join.UserId != Guid.Empty)
      {
        new LogHelper(targetElectionGuid).Add($"Removed full teller - {email ?? join.InviteEmail}", true);
      }
      else
      {
        new LogHelper(targetElectionGuid).Add($"Removed pending full teller - {email ?? join.InviteEmail ?? "?"}", true);
      }

      //TODO notify this user and sign them out!
      return new
      {
        Success = true
      }.AsJsonResult();
    }

    [ForAuthenticatedTeller]
    public JsonResult AddFullTeller(string email, Guid election)
    {
      // ensure that I am the owner of the election
      var myJoin = Db.JoinElectionUser
        .FirstOrDefault(je => je.ElectionGuid == election && je.UserId == UserSession.UserGuid);
      if (myJoin == null || myJoin.Role != null)
      {
        return new
        {
          Success = false,
          Message = "Adding not allowed"
        }.AsJsonResult();
      }

      // don't bother checking if already invited -- no harm in duplicate and JS has screened

      var jeu = new JoinElectionUser
      {
        ElectionGuid = election,
        UserId = Guid.Empty, // will be filled when the user logs in
        Role = "Full",
        InviteEmail = email,
      };

      Db.JoinElectionUser.Add(jeu);

      Db.SaveChanges();

      new LogHelper(election).Add($"Registered full teller - {email}", true);

      var user = new
      {
        jeu.Role,
        InviteWhen = (DateTime?)null,
        jeu.InviteEmail,
        jeu.C_RowId,
        Email = (string)null,
        UserName = "PENDING", // should match the default user in the DB. Okay if not.
        LastActivityDate = (DateTime?)null,
        isCurrentUser = false
      };

      return new
      {
        Success = true,
        user
      }.AsJsonResult();
    }


    [ForAuthenticatedTeller]
    public JsonResult SendInvitation(int joinId)
    {
      var join = Db.JoinElectionUser
        .FirstOrDefault(je => je.C_RowId == joinId);

      if (join == null)
      {
        return new
        {
          Success = false,
          Message = "Unknown id"
        }.AsJsonResult();
      }

      var targetElectionGuid = join.ElectionGuid;

      // ensure that I am the owner of the election
      var myJoin = Db.JoinElectionUser
        .FirstOrDefault(je => je.ElectionGuid == targetElectionGuid && je.UserId == UserSession.UserGuid);
      if (myJoin == null || myJoin.Role != null)
      {
        return new
        {
          Success = false,
          Message = "Only owners can send invitations"
        }.AsJsonResult();
      }

      var election = new ElectionsListViewModel()
        .MyElections()
        .FirstOrDefault(e => e.ElectionGuid == targetElectionGuid);
      if (election == null)
      {
        return new
        {
          Success = false,
          Message = "Invalid election"
        }.AsJsonResult();
      }

      return new EmailHelper().SendFullTellerInvitation(election, join.InviteEmail);
    }

  }
}