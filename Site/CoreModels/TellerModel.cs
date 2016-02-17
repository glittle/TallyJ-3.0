using System;
using System.Collections.Generic;
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
    public JsonResult GrantAccessToGuestTeller(Guid electionGuid, string codeToTry, Guid oldComputerGuid)
    {
      var electionModel = new ElectionModel();

      var passcode = new PublicElectionLister().GetPasscodeIfAvailable(electionGuid);
      if (passcode == null)
      {
        return new
                 {
                   Error = "Sorry, unknown election id"
                 }.AsJsonResult();
      }
      if (passcode != codeToTry)
      {
        return new
                 {
                   Error = "Sorry, invalid code entered"
                 }.AsJsonResult();
      }

      if (!UserSession.IsLoggedIn)
      {
        var fakeUserName = HttpContext.Current.Session.SessionID.Substring(0, 5) + Guid.NewGuid().ToString().Substring(0, 5);
        FormsAuthentication.SetAuthCookie(fakeUserName, true);
        UserSession.IsGuestTeller = true;
      }

      electionModel.JoinIntoElection(electionGuid, oldComputerGuid);

      return new
               {
                 LoggedIn = true,
                 CompGuid = UserSession.CurrentComputer.ComputerGuid
               }.AsJsonResult();
    }

    public object ChooseTeller(int num, int tellerId, string newName)
    {
      var helper = new TellerHelper();

      var tellerCacher = new TellerCacher(Db);
      var computerCacher = new ComputerCacher();

      var currentComputer = UserSession.CurrentComputer;

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

        computerCacher.UpdateComputer(currentComputer);

        return new { Saved = true };
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
            Name = newName,
            UsingComputerCode = UserSession.CurrentComputerCode,
          };
          Db.Teller.Add(teller);
          Db.SaveChanges();
          tellerCacher.UpdateItemAndSaveCache(teller);
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
      }

      switch (num)
      {
        case 1:
          currentComputer.Teller1 = teller.Name;
          break;
        case 2:
          currentComputer.Teller2 = teller.Name;
          break;
      }
      Db.SaveChanges();
      computerCacher.UpdateComputer(currentComputer);

      UserSession.SetCurrentTeller(num, teller.Name);

      return new
      {
        Saved = true,
        Selected = teller.C_RowId,
        TellerList = helper.GetTellerOptions(num)
      };

    }

    //    public static string GetTellerNames(Guid? tellerGuid1, Guid? tellerGuid2)
    //    {
    //      var tellers = new TellerCacher(Db).AllForThisElection;
    //
    //      var tellersOnThisComputer = new List<Teller>
    //      {
    //        tellers.FirstOrDefault(t => t.TellerGuid == tellerGuid1),
    //        tellers.FirstOrDefault(t => t.TellerGuid == tellerGuid2)
    //      };
    //      return tellersOnThisComputer.Select(t => t == null ? "" : t.Name).JoinedAsString(", ", true);
    //    }

    public static string GetTellerNames(string teller1, string teller2)
    {
      var tellersOnThisComputer = new List<string>
      {
        teller1,
        teller2
      };
      return tellersOnThisComputer.JoinedAsString(", ", true);
    }


    public object DeleteTeller(int tellerId)
    {
      var thisTeller = new TellerCacher(Db).GetById(tellerId);

      if (thisTeller == null)
      {
        return new { Deleted = false, Error = "Not found" };
      }

      try
      {
        Db.Teller.Attach(thisTeller);
        Db.Teller.Remove(thisTeller);
        Db.SaveChanges();

        new TellerCacher(Db).RemoveItemAndSaveCache(thisTeller);
      }
      catch (Exception ex)
      {
        return new { Deleted = false, Error = ex.Message };
      }

      return new { Deleted = true };
    }
  }
}