using System;
using System.Linq;
using System.Web;
using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;
using TallyJ.Models;

namespace TallyJ.Code.Session
{
  public static class UserSession
  {
    /// <summary>
    ///     Logged in identity name.
    /// </summary>
    public static string LoginId
    {
      get { return HttpContext.Current.User.Identity.Name ?? ""; }
    }

    private static bool UserGuidHasBeenLoaded
    {
      get { return SessionKey.UserGuidRetrieved.FromSession(false); }
      set { CurrentContext.Session[SessionKey.UserGuidRetrieved] = value; }
    }

    public static Guid UserGuid
    {
      get
      {
        if (!UserGuidHasBeenLoaded && LoginId.HasContent())
        {
          var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;
          var user = db.Users.Where(u => u.UserName == LoginId).SingleOrDefault();

          UserGuidHasBeenLoaded = true;

          if (user != null)
          {
            CurrentContext.Session[SessionKey.CurrentUserGuid] = user.UserId;
          }
        }
        return SessionKey.CurrentUserGuid.FromSession(Guid.Empty);
      }
    }

    /// <summary>
    ///     Has this person logged in?
    /// </summary>
    public static bool IsLoggedIn
    {
      get { return LoginId.HasContent(); }
    }

    public static Guid CurrentLocationGuid
    {
      get { return SessionKey.CurrentLocationGuid.FromSession(Guid.Empty); }
    }

    public static string CurrentLocationName
    {
      get { return SessionKey.CurrentLocationName.FromSession("[No location selected]"); }
    }

    /// <summary>
    ///     The current election, as stored in Session.  Could be null.
    /// </summary>
    public static Election CurrentElection
    {
      get { return SessionKey.CurrentElection.FromSession<Election>(null); }
    }

    /// <summary>
    ///     Title of election, if one is selected
    /// </summary>
    public static string CurrentElectionName
    {
      get { return CurrentElection == null ? "[No election selected]" : CurrentElection.Name; }
    }

    public static Guid CurrentElectionGuid
    {
      get
      {
        var current = CurrentElection;
        return current == null ? Guid.Empty : current.ElectionGuid;
      }
    }

    /// <summary>
    /// </summary>
    public static long LastVersionNum
    {
      get { return SessionKey.LastVersionNum.FromSession(0); }
      set { SessionKey.LastVersionNum.SetInSession(value); }
    }

    public static int ComputerRowId
    {
      get { return SessionKey.CurrentComputerRowId.FromSession(0); }
      set { SessionKey.CurrentComputerRowId.SetInSession(value); }
    }

    public static void ProcessLogin(string userName)
    {
      ComputerRowId = new ComputerModel().CreateComputerRecord().C_RowId;
    }

    public static void ProcessLogout()
    {
      new ComputerModel().DeleteAtLogout(ComputerRowId);
    }
  }
}