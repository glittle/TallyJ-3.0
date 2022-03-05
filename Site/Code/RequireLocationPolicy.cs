using System;
using System.Web;
using FluentSecurity;
using FluentSecurity.Policy;
using TallyJ.Code.Session;

namespace TallyJ.Code
{
  public class RequireLocationPolicy : ISecurityPolicy
  {
    #region ISecurityPolicy Members

    public PolicyResult Enforce(ISecurityContext context)
    {
      // if logged in and does have a current election, okay
      if (UserSession.CurrentLocationGuid != Guid.Empty)
      {
        return PolicyResult.CreateSuccessResult(this);
      }

      // determine if they are logged in or not
      var url = VirtualPathUtility.ToAbsolute(UserSession.IsLoggedInTeller ? "~/Dashboard/ElectionList" : "~/");

      var response = HttpContext.Current.Response;
      response.Clear();
      response.Redirect(url);
      response.End();

      return PolicyResult.CreateFailureResult(this, "Location must be selected");
    }

    #endregion
  }
}