using System;
using System.Data.Objects.SqlClient;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using TallyJ.Code;
using TallyJ.Code.Session;
using System.Linq;

namespace TallyJ.Models
{
  public class TellerModel : DataConnectedModel
  {
    public JsonResult GrantAccessToGuestTeller(int electionId, string secretCode)
    {
      var model = new ElectionModel();

      var desiredElection = model.VisibleElectionInfo1().SingleOrDefault(e => e.C_RowId == electionId
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
  }
}