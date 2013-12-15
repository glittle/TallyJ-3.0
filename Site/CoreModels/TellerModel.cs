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

      var thisTeller = new TellerCacher().GetById(tellerId);

      var currentComputer = UserSession.CurrentComputer;

      switch (tellerId)
      {
        case 0:
          UserSession.SetCurrentTeller(num, null);
          if (thisTeller != null)
          {
            thisTeller.UsingComputerCode = "";
            Db.Computer.Attach(currentComputer);
            switch (num)
            {
              case 1:
                currentComputer.Teller1 = null;
                break;
              case 2:
                currentComputer.Teller2 = null;
                break;
            }

            new TellerCacher().UpdateItemAndSaveCache(thisTeller);
            new ComputerCacher().UpdateItemAndSaveCache(currentComputer);

            Db.SaveChanges();
          }
          return new { Saved = true };

        case -1:
          // add new

          // check for existing
          var matchedTeller = helper.Tellers.FirstOrDefault(t => t.Name == newName);
          if (matchedTeller != null)
          {
            UserSession.SetCurrentTeller(num, matchedTeller.TellerGuid);
            return new { Saved = true };
          }

          // add the new one
          var teller = new Teller
                         {
                           ElectionGuid = UserSession.CurrentElectionGuid,
                           TellerGuid = Guid.NewGuid(),
                           Name = newName,
                           UsingComputerCode = UserSession.CurrentComputerCode,
                         };
          Db.Teller.Add(teller);

          Db.Computer.Attach(currentComputer);
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

          new TellerCacher().AddItemAndSaveCache(teller);
          new ComputerCacher().UpdateItemAndSaveCache(currentComputer);

          return new
                   {
                     Saved = true,
                     Selected = teller.C_RowId,
                     TellerList = helper.GetTellerOptions(num)
                   };

        default:
          // use existing
          if (thisTeller != null)
          {
            UserSession.SetCurrentTeller(num, thisTeller.TellerGuid);
            thisTeller.UsingComputerCode = UserSession.CurrentComputerCode;

            Db.Computer.Attach(currentComputer);
            switch (num)
            {
              case 1:
                currentComputer.Teller1 = thisTeller.TellerGuid;
                break;
              case 2:
                currentComputer.Teller2 = thisTeller.TellerGuid;
                break;
            }

            Db.SaveChanges();

            new TellerCacher().UpdateItemAndSaveCache(thisTeller);
            new ComputerCacher().UpdateItemAndSaveCache(currentComputer);
          }
          return new { Saved = true };
      }

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
        Db.Teller.Remove(thisTeller);
        Db.SaveChanges();
      }
      catch (Exception ex)
      {
        return new { Deleted = false, Error = ex.Message };
      }

      new TellerCacher().RemoveItemAndSaveCache(thisTeller);

      return new { Deleted = true };

    }
  }
}