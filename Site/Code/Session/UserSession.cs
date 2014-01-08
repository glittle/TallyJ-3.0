using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using TallyJ.Code.Data;
using TallyJ.Code.Enumerations;
using TallyJ.Code.UnityRelated;
using TallyJ.CoreModels;
using TallyJ.EF;
using Membership = System.Web.Security.Membership;

namespace TallyJ.Code.Session
{
  public static class UserSession
  {
    /// <summary>
    ///   Logged in identity name.
    /// </summary>
    public static string LoginId
    {
      get { return HttpContext.Current.User.Identity.Name ?? ""; }
    }

    /// <Summary>May be null if not logged in.</Summary>
    public static MembershipUser MemberInfo
    {
      get
      {
        try
        {
          return Membership.GetUser(LoginId);
        }
        catch (Exception)
        {
          // likely not logged in yet
          return null;
        }
      }
    }

    public static string MemberEmail
    {
      get
      {
        var info = MemberInfo;
        return info == null ? "" : info.Email;
      }
    }

    public static string MemberName
    {
      get
      {
        var info = MemberInfo;
        return info == null ? "" : info.UserName;
      }
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
          var user = db.Users.SingleOrDefault(u => u.UserName == LoginId);

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
    ///   Has this person logged in?
    /// </summary>
    public static bool IsLoggedIn
    {
      get { return LoginId.HasContent(); }
    }

    /// <summary>
    ///   The current election, as stored in Page items.  On first access, is loaded from DB. Could be null.  Setting this also sets the CurrentElectionGuid into Session.
    /// </summary>
    public static Election CurrentElection
    {
      get
      {
        var current = HttpContext.Current.Items[ItemKey.CurrentElection] as Election;
        if (current != null)
        {
          return current;
        }

        // even if have valid guid, may be null if election was just deleted
        var currentElection = CurrentElectionGuid.HasContent() ? new ElectionCacher().AllForThisElection.FirstOrDefault() : null;

        if (currentElection == null)
        {
          CurrentElectionGuid = Guid.Empty;
        }

        HttpContext.Current.Items[ItemKey.CurrentElection] = currentElection;
        return currentElection;
      }
    }

    /// <summary>
    ///   Title of election, if one is selected
    /// </summary>
    public static string CurrentElectionName
    {
      get
      {
        var current = CurrentElection;
        return current == null ? "" : current.Name;
      }
    }

    /// <summary>
    ///   Title of election, if one is selected
    /// </summary>
    public static string CurrentElectionDisplayNameAndInfo
    {
      get
      {
        var current = CurrentElection;
        if (current == null)
          return "";

        var type = ElectionTypeEnum.TextFor(current.ElectionType);
        var mode = ElectionModeEnum.TextFor(current.ElectionMode);

        var modeWithNum = mode;
        var numToElect = current.NumberToElect.AsInt();
        if (numToElect != 9)
        {
          modeWithNum = "{0}for {1} member{2}".FilledWith(mode.SurroundContentWith("", " "), current.NumberToElect,
                                                          numToElect.Plural());
        }

        return "{0}{1} - \"{2}\"".FilledWith(type, modeWithNum.SurroundContentWith(" (", ")"), current.Name);
      }
    }

    /// <Summary>Stored as Guid in session</Summary>
    public static Guid CurrentElectionGuid
    {
      get { return SessionKey.CurrentElectionGuid.FromSession(Guid.Empty); }
      set { SessionKey.CurrentElectionGuid.SetInSession(value); }
    }

    public static Location CurrentLocation
    {
      get { return new LocationCacher().AllForThisElection.FirstOrDefault(l => l.LocationGuid == CurrentLocationGuid); }
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
      get { return SessionKey.CurrentLocationGuid.FromSession(Guid.Empty); }
      set { SessionKey.CurrentLocationGuid.SetInSession(value); }
    }

    public static string CurrentBallotFilter
    {
      get { return SessionKey.CurrentBallotFilter.FromSession(""); }
      set { SessionKey.CurrentBallotFilter.SetInSession(value); }
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
      get
      {
        var currentComputerId = CurrentComputerId;
        return currentComputerId == 0 ? null : new ComputerCacher().GetById(currentComputerId);
      }
    }

    public static int CurrentComputerId
    {
      get { return SessionKey.CurrentComputerId.FromSession(0); }
      set { SessionKey.CurrentComputerId.SetInSession(value); }
    }

    public static string CurrentComputerCode
    {
      get
      {
        var current = CurrentComputer;
        return current == null ? "" : current.ComputerCode;
      }
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
        IsGuestTeller = !value;
      }
    }

    public static string WebProtocol
    {
      get { return new SiteInfo().CurrentEnvironment == "AppHarbor" ? "https" : "http"; }
    }

    /// <Summary>Has the client/server time difference been figured out?</Summary>
    public static bool TimeOffsetKnown
    {
      get { return SessionKey.TimeOffsetKnown.FromSession(false); }
      set { SessionKey.TimeOffsetKnown.SetInSession(value); }
    }

    /// <Summary>Has the client/server time difference been figured out?</Summary>
    public static int TimeOffsetServerAhead
    {
      get { return SessionKey.TimeOffset.FromSession(0); }
      set { SessionKey.TimeOffset.SetInSession(value); }
    }

    public static string CurrentElectionStatusName
    {
      get
      {
        var election = CurrentElection;
        return election == null
                 ? ElectionTallyStatusEnum.NotStarted
                 : ElectionTallyStatusEnum.TextFor(election.TallyStatus);
      }
    }

    public static string CurrentElectionStatus
    {
      get
      {
        var election = CurrentElection;
        return election == null || election.TallyStatus.HasNoContent()
                 ? ElectionTallyStatusEnum.NotStarted
                 : election.TallyStatus;
      }
    }

    public static Guid? GetCurrentTeller(int num)
    {
      var stored = HttpContext.Current.Session[SessionKey.CurrentTeller + num];
      return stored == null ? null : (Guid?)stored;
    }

    public static void SetCurrentTeller(int num, Guid? tellerGuid)
    {
      HttpContext.Current.Session[SessionKey.CurrentTeller + num] = tellerGuid;
    }

    public static void ProcessLogin()
    {
      HttpContext.Current.Session.Clear();
      // UserSession.CurrentComputerCode = new ComputerModel().CreateComputerRecordForMe();
    }

    public static void ProcessLogout()
    {
      new ComputerModel().DeleteAtLogout(CurrentComputerId);
    }

    public static bool IsFeatured(string pageFeatureWhen)
    {
      if (pageFeatureWhen == "*")
      {
        return true;
      }
      var election = CurrentElection;
      var currentStatus = election == null
                            ? ElectionTallyStatusEnum.NotStarted
                            : election.TallyStatus ?? ElectionTallyStatusEnum.NotStarted;

      return pageFeatureWhen.Contains(currentStatus);
    }
  }
}