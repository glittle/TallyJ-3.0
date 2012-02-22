using System;
using System.Data.Objects.SqlClient;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using TallyJ.Code;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;
using System.Linq;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class TellerModel : DataConnectedModel
  {
    public JsonResult GrantAccessToGuestTeller(int electionId, string secretCode)
    {
      var model = new ElectionModel();

      var desiredElection = model.VisibleElections().SingleOrDefault(e => e.C_RowId == electionId
                                                              && e.ElectionPasscode == secretCode);

      if (desiredElection == null)
      {
        return new
                 {
                   Error = "Sorry, that election is not available"
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

      var thisTeller = helper.Tellers.SingleOrDefault(t => t.C_RowId == tellerId);

      switch (tellerId)
      {
        case 0:
          UserSession.SetCurrentTeller(num, null);
          if (thisTeller != null)
          {
            thisTeller.UsingComputerCode = "";
            Db.Computers.Attach(UserSession.CurrentComputer);
            switch (num)
            {
              case 1:
                UserSession.CurrentComputer.Teller1 = null;
                break;
              case 2:
                UserSession.CurrentComputer.Teller2 = null;
                break;
            }
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
          Db.Tellers.Add(teller);

          Db.Computers.Attach(UserSession.CurrentComputer);
          switch (num)
          {
            case 1:
              UserSession.CurrentComputer.Teller1 = teller.TellerGuid;
              break;
            case 2:
              UserSession.CurrentComputer.Teller2 = teller.TellerGuid;
              break;
          }

          Db.SaveChanges();
          UserSession.SetCurrentTeller(num, teller.TellerGuid);
          helper.RefreshTellerList();

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

            Db.Computers.Attach(UserSession.CurrentComputer);
            switch (num)
            {
              case 1:
                UserSession.CurrentComputer.Teller1 = thisTeller.TellerGuid;
                break;
              case 2:
                UserSession.CurrentComputer.Teller2 = thisTeller.TellerGuid;
                break;
            }

            Db.SaveChanges();
          }
          return new { Saved = true };
      }

    }

    public object DeleteTeller(int tellerId)
    {
      var helper = new TellerHelper();

      var thisTeller = helper.Tellers.SingleOrDefault(t => t.C_RowId == tellerId);

      if (thisTeller == null)
      {
        return new {Deleted=false, Error="Not found"};
      }

      try
      {
        Db.Tellers.Remove(thisTeller);
        Db.SaveChanges();
      }
      catch (Exception ex)
      {
        return new { Deleted = false, Error = ex.Message };
      }

      return new { Deleted = true };

    }
  }
}