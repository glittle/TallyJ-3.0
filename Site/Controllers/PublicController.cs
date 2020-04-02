using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  public class PublicController : Controller
  {
    //
    // GET: /Public/

    public ActionResult Index()
    {
      //      var identity1 = (ClaimsIdentity)User.Identity;
      //      IEnumerable<Claim> claims1 = identity1.Claims;
      //      var numClaims1 = claims1.Count();
      //
      //      var identity2 = (ClaimsPrincipal)Thread.CurrentPrincipal;
      //      var claims2 = identity2.Claims;
      //      var numClaims2 = claims2.Count();
      return View("Home");
    }

    public ActionResult About()
    {
      return View();
    }

    public ActionResult Contact()
    {
      return View();
    }

    public ActionResult Privacy()
    {
      return View();
    }

    public ActionResult Learning()
    {
      return View();
    }

    [Route("Login")]
    public ActionResult VoterLogin()
    {
      return View();
    }
    
    // public JsonResult DoScheduled(int minutes = 5)
    // {
    //   var now = DateTime.Now;
    //   if (minutes < 0 || new SiteInfo().CurrentEnvironment == "Dev")
    //   {
    //     minutes = 0;
    //   }
    //   if (now - MvcApplication.LastRunOfScheduled <= TimeSpan.FromMinutes(minutes))
    //   {
    //     // ignore if repeated too soon
    //     return new { msg = "not expected" }.AsJsonResult();
    //   }
    //
    //   MvcApplication.LastRunOfScheduled = now;
    //
    //   return new EmailHelper().DoScheduled();
    // }

    [AllowAnonymous]
    public FilePathResult FavIcon()
    {
      return new FilePathResult("~/images/favicon.ico", "image/x-icon");
    }

    public JsonResult Heartbeat(PulseInfo info)
    {
      return new PulseModel(this, info).ProcessPulseJson();
    }

    public ActionResult Install()
    {
      return View();
    }

    public JsonResult Warmup()
    {
      // force the server to contact the database to ensure that it is warmed up and ready for action
      var dummy = UserSession.GetNewDbContext.Election.FirstOrDefault();
      return null;
    }

    public JsonResult TellerJoin(Guid electionGuid, string pc, Guid? oldCompGuid)
    {
      return new TellerModel().GrantAccessToGuestTeller(electionGuid, pc, oldCompGuid.AsGuid());
    }

    public JsonResult GetTimeOffset(long now = 0)
    {
      if (now == 0)
      {
        // not called by our code?
        return null;
      }

      // adjust client time by .5 seconds to allow for network and server time
      const double fudgeFactor = .5 * 1000;
      var clientTimeNow = new DateTime(1970, 1, 1).AddMilliseconds(now + fudgeFactor);
      var serverTime = DateTime.Now;
      var diff = (serverTime - clientTimeNow).TotalMilliseconds;
      UserSession.TimeOffsetServerAhead = diff.AsInt();
      UserSession.TimeOffsetKnown = true;
      return new
      {
        timeOffset = diff
      }.AsJsonResult();
    }


    public JsonResult OpenElections()
    {
      return new
      {
        html = new PublicElectionLister().RefreshAndGetListOfAvailableElections()
      }.AsJsonResult();
    }

    public JsonResult PublicHub(string connId)
    {
      new PublicHub().Join(connId);
      return OpenElections();
    }

    public void JoinMainHub(string connId, string electionGuid)
    {
      // removed [Authorize]... just ignore if we don't like the call

      if (UserSession.CurrentElectionGuid == Guid.Empty)
      {
        return;
      }
      if (electionGuid.AsGuid() == Guid.Empty)
      {
        return;
      }

      new MainHub().Join(connId);
    }

  }

}
