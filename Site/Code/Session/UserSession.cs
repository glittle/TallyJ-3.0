using System;
using System.Linq;
using System.Web;
using System.Web.Providers.Entities;
using System.Web.Security;
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

    /// <summary>
    ///     The current election, as stored in Session.  Could be null.
    /// </summary>
    public static Election CurrentElection
    {
      get { return SessionKey.CurrentElection.FromSession<Election>(null); }
      set { SessionKey.CurrentElection.SetInSession(value); }
    }

    /// <summary>
    ///     Title of election, if one is selected
    /// </summary>
    public static string CurrentElectionName
    {
      get
      {
        var current = CurrentElection;
        return current == null ? "[No election selected]" : current.Name;
      }
    }

    public static Guid CurrentElectionGuid
    {
      get
      {
        var current = CurrentElection;
        return current == null ? Guid.Empty : current.ElectionGuid;
      }
    }

    public static Location CurrentLocation
    {
      get { return SessionKey.CurrentLocation.FromSession<Location>(null); }
    }

    public static string CurrentLocationName
    {
      get
      {
        var current = CurrentLocation;
        return current == null ? "[No location selected]" : current.Name;
      }
    }

    public static Guid CurrentLocationGuid
    {
      get
      {
        var current = CurrentLocation;
        return current == null ? Guid.Empty : current.LocationGuid;
      }
    }

    /// <summary>
    /// </summary>
    public static long LastVersionNum
    {
      get { return SessionKey.LastVersionNum.FromSession(0); }
      set { SessionKey.LastVersionNum.SetInSession(value); }
    }

    public static Computer CurrentComputer
    {
      get { return SessionKey.CurrentComputer.FromSession<Computer>(null); }
    }

    public static int ComputerRowId
    {
      get
      {
        var current = CurrentComputer;
        return current == null ? 0 : current.C_RowId;
      }
    }

    public static string CurrentComputerCode
    {
      get
      {
        var current = CurrentComputer;
        return current == null ? "" : current.ComputerCode;
      }
    }

    public static Guid? CurrentTellerAtKeyboard
    {
      get { return null; }
    }

    public static Guid? CurrentTellerAssisting
    {
      get { return null; }
    }

    public static void ProcessLogin()
    {
      HttpContext.Current.Session.Clear();
      SessionKey.CurrentComputer.SetInSession(new ComputerModel().CreateComputerRecordForMe());
    }

    public static void ProcessLogout()
    {
      new ComputerModel().DeleteAtLogout(ComputerRowId);
      HttpContext.Current.Session.Clear();
    }

    /// <Summary>defaults to true</Summary>
    public static bool IsGuestTeller
    {
      get { return SessionKey.IsGuestTeller.FromSession(true); }
      set { SessionKey.IsGuestTeller.SetInSession(value); }
    }

    /// <Summary>If logged in with an account</Summary>
    public static bool IsKnownTeller
    {
      get { return SessionKey.IsKnownTeller.FromSession(false); }
      set
      {
        SessionKey.IsKnownTeller.SetInSession(value);
        if (value)
        {
          IsGuestTeller = false;
        }
      }
    }

    public static string WebProtocol
    {
      get
      {
        return new SiteInfo().CurrentEnvironment == "AppHarbor" ? "https" : "http";
      }
    }
  }
}