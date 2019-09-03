using System.Web.Mvc;
using TallyJ.Code.Session;

namespace TallyJ.Code
{
  public class AllowVoterAttribute : ActionFilterAttribute
  {
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
      var okay = UserSession.IsVoter && SettingsHelper.HostSupportsOnlineElections;
      if (!okay)
      {
        filterContext.Result = new RedirectResult("~/");
        filterContext.HttpContext.Response.StatusCode = 302;
        //        filterContext.Result = new HttpUnauthorizedResult();
      }
    }
  }
}