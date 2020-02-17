using System.Web;
using FluentSecurity;
using FluentSecurity.Policy;
using TallyJ.Code.Session;

namespace TallyJ.Code
{
  public class RequireElectionPolicy : ISecurityPolicy
  {
    #region ISecurityPolicy Members

    public PolicyResult Enforce(ISecurityContext context)
    {
      // if logged in and does have a current election, okay
      if (UserSession.CurrentElection != null)
      {
        return PolicyResult.CreateSuccessResult(this);
      }

      // determine if they are logged in or not
      var url = VirtualPathUtility.ToAbsolute(UserSession.IsLoggedInTeller ? "~/Dashboard/ChooseElection" : "~/");

      var response = HttpContext.Current.Response;
      response.Clear();
      response.Redirect(url);
      response.End();

      return PolicyResult.CreateFailureResult(this, "Election must be selected");
    }

    #endregion
  }
}