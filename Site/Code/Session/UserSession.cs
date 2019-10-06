﻿using System;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Security;
using TallyJ.Code.Data;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.UnityRelated;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.Code.Session
{
  public static class UserSession
  {
    public static ICurrentContext CurrentContext => UnityInstance.Resolve<ICurrentContext>();

    public static ITallyJDbContext GetNewDbContext => UnityInstance.Resolve<IDbContextFactory>().GetNewDbContext;

    /// <summary>
    ///   Logged in identity name.
    /// </summary>
    public static string LoginId
    {
      get
      {
        return ActivePrincipal.FindFirst("UserName")?.Value;
        //            return HttpContext.Current.User.Identity.Name ?? "";
      }
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
          var user = GetNewDbContext.Users.SingleOrDefault(u => u.UserName == LoginId);

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

    /// <Summary>Stored as Guid in session</Summary>
    public static Guid CurrentElectionGuid
    {
      get
      {
        if (CurrentContext.Session.IsAvailable)
        {
          return SessionKey.CurrentElectionGuid.FromSession(Guid.Empty);
        }

        return Guid.Empty;
      }
      set
      {
        SessionKey.CurrentElectionGuid.SetInSession(value);

        // reset so we don't use data we just loaded
        ItemKey.CurrentElection.SetInPageItems<Election>(null);
        //        HttpContext.Current.Items[ItemKey.CurrentElection] = null;
      }
    }

    /// <summary>
    ///   The current election, as stored in Page items.  On first access, is loaded from DB. Could be null.  Setting this also
    ///   sets the CurrentElectionGuid into Session.
    /// </summary>
    public static Election CurrentElection
    {
      get
      {
        // check temp cache for page rendering
        //var election = HttpContext.Current.Items[ItemKey.CurrentElection] as Election;
        var election = ItemKey.CurrentElection.FromPageItems<Election>(null);
        if (election != null)
        {
          return election;
        }

        var currentElectionGuid = CurrentElectionGuid;
        var hasElection = currentElectionGuid.HasContent();

        if (hasElection)
        {
          var cacher = new ElectionCacher();
          election = cacher.AllForThisElection.FirstOrDefault();
          if (election != null && election.ElectionGuid != currentElectionGuid)
          {
            // occasionally, when changing elections, the cacher has the old election...need to flush it
            cacher.DropThisCache();
            election = cacher.AllForThisElection.FirstOrDefault();
          }

          // even if have valid guid, may be null if election was just deleted
          if (election == null)
          {
            CurrentElectionGuid = Guid.Empty;
          }
          else
          {
            // save for next use in this same rendering
            //            HttpContext.Current.Items[ItemKey.CurrentElection] = election;
            ItemKey.CurrentElection.SetInPageItems(election);
          }
        }

        return election;
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

        return "\"{2}\" - {0}{1}".FilledWith(type, modeWithNum.SurroundContentWith(" (", ")"), current.Name);
      }
    }

    public static Guid CurrentLocationGuid
    {
      get => SessionKey.CurrentLocationGuid.FromSession(Guid.Empty);
      set => SessionKey.CurrentLocationGuid.SetInSession(value);
    }

    public static ClaimsPrincipal ActivePrincipal
    {
      get
      {
        var threadPrincipal = (ClaimsPrincipal)Thread.CurrentPrincipal;
        if (threadPrincipal.Identity.IsAuthenticated)
        {
          return threadPrincipal;
        }

        try
        {
          var owinPrincipal = HttpContext.Current.GetOwinContext().Authentication.AuthenticationResponseGrant?.Principal;
          if (owinPrincipal != null && owinPrincipal.Identity.IsAuthenticated)
          {
            return owinPrincipal;
          }
        }
        catch
        {
          // won't work in some cases
        }

        // the OwinContext principal sometimes does not exist!  Storing it also in Session to be able to get it again.
        var activePrincipal = SessionKey.ActivePrincipal.FromSession(new ClaimsPrincipal(new ClaimsIdentity()));

        return activePrincipal;
      }
    }

    public static bool IsAuthenticated => ActivePrincipal.Identity.IsAuthenticated;

    public static string VoterEmail => ActivePrincipal.FindFirst("Email")?.Value;
    public static string VoterAuthSource => ActivePrincipal.FindFirst("Source")?.Value;
    public static bool VoterIsVerified => ActivePrincipal.FindFirst("IsVoter")?.Value == "True";

    public static DateTime VoterLastLogin
    {
      get => SessionKey.VoterLastLogin.FromSession(DateTime.MinValue);
      set => SessionKey.VoterLastLogin.SetInSession(value);
    }

    public static void RecordLogin(string source, string email)
    {
      if (email.HasNoContent())
      {
        throw new ApplicationException("Email required");
      }

      var db = GetNewDbContext;
      var onlineVoter = db.OnlineVoter.FirstOrDefault(ov => ov.Email == email);
      var now = DateTime.Now;

      if (onlineVoter == null)
      {
        onlineVoter = new OnlineVoter
        {
          Email = email,
          WhenRegistered = now
        };
        db.OnlineVoter.Add(onlineVoter);
      }
      else
      {
        UserSession.VoterLastLogin = onlineVoter.WhenLastLogin.GetValueOrDefault(DateTime.MinValue);
      }

      onlineVoter.WhenLastLogin = now;
      db.SaveChanges();

      new LogHelper().Add($"Login from {source}", true, email);

      new VoterPersonalHub().Login(email); // in case same email is logged into a different computer
    }

    public static Location CurrentLocation
    {
      get
      {
        var currentLocationGuid = CurrentLocationGuid;
        if (currentLocationGuid == Guid.Empty)
        {
          return null;
        }

        var location = ItemKey.CurrentLocation.FromPageItems<Location>(null);
        if (location != null && location.LocationGuid == currentLocationGuid)
        {
          return location;
        }

        var locations = new LocationCacher().AllForThisElection;

        location = locations.FirstOrDefault(l => l.LocationGuid == currentLocationGuid);
        if (location == null)
        {
          if (locations.Count > 1)
          {
            return null;
          }
          location = locations[0];
        }

        if (location.LocationGuid != currentLocationGuid)
        {
          CurrentLocationGuid = location.LocationGuid;
        }

        ItemKey.CurrentLocation.SetInPageItems(location);
        return location;
      }
    }

    public static string CurrentLocationName
    {
      get
      {
        var current = CurrentLocation;
        return current == null ? "[No location selected]" : current.Name;
      }
    }

    public static string CurrentBallotFilter
    {
      get { return SessionKey.CurrentBallotFilter.FromSession(""); }
      set { SessionKey.CurrentBallotFilter.SetInSession(value); }
    }

    //    public static long LastVersionNum
    //    {
    //      get { return SessionKey.LastVersionNum.FromSession(0); }
    //      set { SessionKey.LastVersionNum.SetInSession(value); }
    //    }

    //    public static Computer CurrentComputerX
    //    {
    //      get
    //      {
    //        if (new ComputerCacher(Db).GetById(CurrentComputerId) != null)
    //        {
    //          return new ComputerCacher(Db).GetById(CurrentComputerId);
    //        }
    //        return CurrentElectionGuid.HasContent() ? new ComputerModel().MakeComputerForMe() : null;
    //      }
    //    }
    /// <summary>
    /// </summary>
    /// <summary>
    ///   Gets current computer. If there is none, and we are in an election, will create, save, and return a new one.
    /// </summary>
    public static Computer CurrentComputer
    {
      get
      {
        var currentComputer = SessionKey.CurrentComputer.FromSession<Computer>(null);
        if (currentComputer == null && CurrentElectionGuid != Guid.Empty)
        {
          return new ComputerModel().GetComputerForMe(Guid.Empty);
        }
        return currentComputer;
      }
      set { SessionKey.CurrentComputer.SetInSession(value); }
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
      get { return SessionKey.IsGuestTeller.FromSession(false); }
      set { SessionKey.IsGuestTeller.SetInSession(value); }
    }

    public static string AuthLevel
    {
      get { return IsKnownTeller ? "Known" : IsGuestTeller ? "Guest" : "None"; }
    }

    /// <Summary>If logged in with an account</Summary>
    public static bool IsKnownTeller
    {
      get
      {
        return UserSession.UserGuid != Guid.Empty && SessionKey.IsKnownTeller.FromSession(false);
      }
      set
      {
        SessionKey.IsKnownTeller.SetInSession(value);
        if (value)
        {
          IsGuestTeller = false;
        }
      }
    }

    public static bool IsVoter => VoterIsVerified;

    //    public static string WebProtocol
    //    {
    //      get { return new SiteInfo().CurrentEnvironment == "AppHarbor" ? "https" : "http"; }
    //    }

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

    public static string SiteVersion
    {
      get
      {
        return Properties.Settings.Default.VersionNum;
        //        var versionHtml = MvcViewRenderer.RenderRazorViewToString("~/Views/Shared/Version.cshtml", new object());
        //        var regex = Regex.Match(versionHtml, ".*>(?<version>.*)<.*");
        //        var version = regex.Success ? regex.Groups["version"].Value : "?";
        //        return version;
      }
    }

    public static string FinalizedNoChangesMessage = "Election is Finalized. No further changes allowed!";

    public static string GetCurrentTeller(int num)
    {
      return CurrentContext.Session[SessionKey.CurrentTeller + num] as string;
    }

    public static void SetCurrentTeller(int num, string name)
    {
      CurrentContext.Session[SessionKey.CurrentTeller + num] = name;
    }

    public static void ProcessLogin()
    {
      //      CurrentContext.Session.Clear();
      // UserSession.CurrentComputerCode = new ComputerModel().CreateComputerRecordForMe();
    }

    public static void ProcessLogout()
    {
      new LogHelper().Add("Log Out");

      LeaveElection(false);

      CurrentContext.Session.Clear();
      FormsAuthentication.SignOut();
    }

    /// <summary>
    ///   Leave this election... remove computer record, close election
    /// </summary>
    /// <param name="movingToOtherElection"></param>
    public static void LeaveElection(bool movingToOtherElection)
    {
      var computer = CurrentComputer;
      if (computer != null && computer.AuthLevel == "Known")
      {
        computer.AuthLevel = "Left";
        var computerCacher = new ComputerCacher();
        computerCacher.UpdateComputer(computer);

        var numKnownTellers = computerCacher.ElectionGuidsOfActiveComputers.Count;
        if (numKnownTellers == 0)
        {
          new ElectionModel().CloseElection();
        }
        else
        {
          new PublicHub().TellPublicAboutVisibleElections(); // in case the name, or ListForPublic, etc. has changed
        }
      }


      if (movingToOtherElection)
      {
        ResetWhenSwitchingElections();
      }
    }


    public static bool IsFeatured(string pageFeatureWhen, Election election)
    {
      if (pageFeatureWhen == "*")
      {
        return true;
      }
      var currentStatus = election == null
        ? ElectionTallyStatusEnum.NotStarted
        : election.TallyStatus ?? ElectionTallyStatusEnum.NotStarted;

      return pageFeatureWhen.Contains(currentStatus);
    }

    public static void ResetWhenSwitchingElections()
    {
      new CacherHelper().DropAllCachesForThisElection();

      var session = CurrentContext.Session;

      session.Remove(SessionKey.CurrentBallotFilter);
      session.Remove(SessionKey.CurrentBallotId);
      session.Remove(SessionKey.CurrentComputer);
      session.Remove(SessionKey.CurrentElectionGuid);
      session.Remove(SessionKey.CurrentLocationGuid);
      session.Remove(SessionKey.CurrentTeller + "1");
      session.Remove(SessionKey.CurrentTeller + "2");
    }
  }
}