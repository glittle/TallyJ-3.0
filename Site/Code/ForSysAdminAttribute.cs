using System.Web.Mvc;
using TallyJ.Code.Session;

namespace TallyJ.Code
{
  public class ForSysAdminAttribute : AuthorizeAttribute
  {
    protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
    {
      // IsSysAdmin relies only on a cookie that may last for days
      // IsKnownTeller relies on session, which times out sooner
      return UserSession.IsKnownTeller && UserSession.IsSysAdmin;
    }
  }
}