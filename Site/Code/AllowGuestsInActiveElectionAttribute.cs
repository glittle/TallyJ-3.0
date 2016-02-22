using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Mvc;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

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

      var electionModel = new ElectionModel();

      if (UserSession.IsGuestTeller)
      {
        if (!electionModel.GuestsAllowed())
        {
          UserSession.ProcessLogout();
          return false;
        }
        return true;
      }

      // only update visit every 15 minutes. Lasts for 1 hour, so could be only 45 minutes.
      var currentElection = UserSession.CurrentElection;

      if (currentElection != null && currentElection.ListForPublic.AsBoolean())
      {
        LogTime("init");
        var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;
        LogTime("resolve");

        db.Election.Attach(currentElection);
        LogTime("attach");

        currentElection.ListedForPublicAsOf = DateTime.Now;

        LogTime("listed");
        var electionCacher = new ElectionCacher(db);
        LogTime("cacher");

        electionCacher.UpdateItemAndSaveCache(currentElection);
        LogTime("update cache");

        //if (currentElection.ListForPublic.AsBoolean() &&
        //    (DateTime.Now - currentElection.ListedForPublicAsOf.GetValueOrDefault(DateTime.Now)).TotalMinutes > 15)
        {

          db.SaveChanges();

          LogTime("db save");
        }

        new PublicHub().TellPublicAboutVisibleElections();
        LogTime("notify");
      }

      return true;
    }

    static DateTime _last = DateTime.Now;
    void LogTime(string msg = "")
    {
      //var now = DateTime.Now;
      //var diff = now - _last;
      //Debugger.Log(1, "timing", "{0} {1}\r\n".FilledWith(diff.ToString("c"), msg));
      //_last = now;
    }
  }
}