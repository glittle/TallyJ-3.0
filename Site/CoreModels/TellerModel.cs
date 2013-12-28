using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using TallyJ.Code;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;
using System.Linq;
using TallyJ.EF;


namespace TallyJ.CoreModels
{
  public class TellerModel : DataConnectedModel
  {
    public JsonResult GrantAccessToGuestTeller(int electionId, string secretCode)
    {
      var model = new ElectionModel();

      var desiredElection = new ElectionCacher().PublicElections.SingleOrDefault(e => e.C_RowId == electionId
                                                              && e.ElectionPasscode == secretCode);

      if (desiredElection == null)
      {
        return new
                 {
                   Error = "Sorry, unable to join that election"
                 }.AsJsonResult();
      }

      var fakeUserName = HttpContext.Current.Session.SessionID.Substring(0, 5) + Guid.NewGuid().ToString().Substring(0, 5);

      FormsAuthentication.SetAuthCookie(fakeUserName, false);
      UserSession.ProcessLogin();

      UserSession.IsGuestTeller = true;

      model.JoinIntoElection(desiredElection.ElectionGuid);

      return new
               {
                 LoggedIn = true
               }.AsJsonResult();
    }

    public object ChooseTeller(int num, int tellerId, string newName)
    {
      var helper = new TellerHelper();

      var tellerCacher = new TellerCacher();
      var computerCacher = new ComputerCacher();

      var currentComputer = UserSession.CurrentComputer;
      if (currentComputer == null)
      {
        var computerModel = new ComputerModel();
        computerModel.AddCurrentComputerIntoCurrentElection();

        currentComputer = UserSession.CurrentComputer;
      }
      else
      {
        Db.Computer.Attach(currentComputer);
      }

      if (tellerId == 0)
      {
        UserSession.SetCurrentTeller(num, null);

        switch (num)
        {
          case 1:
            currentComputer.Teller1 = null;
            break;
          case 2:
            currentComputer.Teller2 = null;
            break;
        }

        Db.SaveChanges();
        computerCacher.UpdateItemAndSaveCache(currentComputer);

        return new {Saved = true};
      }

      Teller teller;

      if (tellerId == -1)
      {
        // add new
        // check for existing
        teller =
          tellerCacher.AllForThisElection.FirstOrDefault(t => t.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));
        if (teller == null)
        {
          // add the new one
          teller = new Teller
          {
            ElectionGuid = UserSession.CurrentElectionGuid,
            TellerGuid = Guid.NewGuid(),
            Name = newName,
            UsingComputerCode = UserSession.CurrentComputerCode,
          };
          Db.Teller.Add(teller);
        }
      }
      else
      {
        // using existing
        teller = tellerCacher.GetById(tellerId);
        if (teller == null)
        {
          return new { Saved = false };
        }
        Db.Teller.Attach(teller);
      }

      switch (num)
      {
        case 1:
          currentComputer.Teller1 = teller.TellerGuid;
          break;
        case 2:
          currentComputer.Teller2 = teller.TellerGuid;
          break;
      }

      Db.SaveChanges();

      UserSession.SetCurrentTeller(num, teller.TellerGuid);

      tellerCacher.UpdateItemAndSaveCache(teller);
      computerCacher.UpdateItemAndSaveCache(currentComputer);

      return new
      {
        Saved = true,
        Selected = teller.C_RowId,
        TellerList = helper.GetTellerOptions(num)
      };

    }

    public object DeleteTeller(int tellerId)
    {
      var thisTeller = new TellerCacher().GetById(tellerId);

      if (thisTeller == null)
      {
        return new { Deleted = false, Error = "Not found" };
      }

      try
      {
        Db.Teller.Attach(thisTeller);
        Db.Teller.Remove(thisTeller);
        Db.SaveChanges();

        new TellerCacher().RemoveItemAndSaveCache(thisTeller);
      }
      catch (Exception ex)
      {
        return new { Deleted = false, Error = ex.Message };
      }

      return new { Deleted = true };
    }
  }
}