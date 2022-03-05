using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using FluentSecurity;
using TallyJ.Code.Session;

namespace TallyJ.Code
{
  public class RequireElectionPolicyViolationHandler : IPolicyViolationHandler
  {
    public ActionResult Handle(PolicyViolationException exception)
    {
      //TODO: want to record where we were trying to go

      return new RedirectToRouteResult(new RouteValueDictionary(new
      {
        action = "ElectionList",
        controller = "Dashboard",
        area = ""
      }));
    }
  }
}