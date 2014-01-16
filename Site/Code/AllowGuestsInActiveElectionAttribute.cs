using System.Web.Mvc;
using TallyJ.Code.Session;
using TallyJ.CoreModels;

namespace TallyJ.Code
{
  public class AllowGuestsInActiveElectionAttribute : AuthorizeAttribute
  {
    protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
    {
      var authorized = base.AuthorizeCore(httpContext);
      if (!authorized)
      {
        // The user is not authenticated
        return false;
      }

      if (UserSession.IsGuestTeller && !new ElectionModel().GuestsAllowed())
      {
        UserSession.ProcessLogout();
        return false;
      }
      return true;
    }
  }
}