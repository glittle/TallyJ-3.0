using System.Web.Mvc;
using TallyJ.Code.Session;

namespace TallyJ.Code
{
  public class ForAuthenticatedTellerAttribute : AuthorizeAttribute
  {
    protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
    {
      return UserSession.IsKnownTeller;
    }
  }
}