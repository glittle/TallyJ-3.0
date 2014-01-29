using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ElectionModel : DataConnectedModel
  {
    public ElectionRules GetRules(string type, string mode)
    {
      var rules = new ElectionRules
      {
        Num = 0,
        Extra = 0,
        CanVote = "",
        CanReceive = "",
        IsSingleNameElection = false
      };


      switch (type)
      {
        case "LSA":
          rules.CanVote = CanVoteOrReceive.All;
          rules.CanVoteLocked = true;

          rules.Extra = 0;
          rules.ExtraLocked = true;

          switch (mode)
          {
            case ElectionMode.Normal:
              rules.Num = 9;
              rules.NumLocked = true;
              rules.CanReceive = CanVoteOrReceive.All;
              break;
            case ElectionMode.TieBreak:
              rules.Num = 1;
              rules.NumLocked = false;
              rules.CanReceive = CanVoteOrReceive.NamedPeople;
              break;
            case ElectionMode.ByElection:
              rules.Num = 1;
              rules.NumLocked = false;
              rules.CanReceive = CanVoteOrReceive.All;
              break;
          }
          rules.CanReceiveLocked = true;

          break;

        case "NSA":
          rules.CanVote = CanVoteOrReceive.NamedPeople; // delegates
          rules.CanVoteLocked = true;

          rules.Extra = 0;
          rules.ExtraLocked = true;

          switch (mode)
          {
            case ElectionMode.Normal:
              rules.Num = 9;
              rules.NumLocked = true;
              rules.CanReceive = CanVoteOrReceive.All;
              break;
            case ElectionMode.TieBreak:
              rules.Num = 1;
              rules.NumLocked = false;
              rules.CanReceive = CanVoteOrReceive.NamedPeople;
              break;
            case ElectionMode.ByElection:
              rules.Num = 1;
              rules.NumLocked = false;
              rules.CanReceive = CanVoteOrReceive.All;
              break;
          }

          rules.CanReceiveLocked = true;

          break;

        case "Con":
          rules.CanVote = CanVoteOrReceive.All;
          rules.CanVoteLocked = true;

          switch (mode)
          {
            case ElectionMode.Normal:
              rules.Num = 5;
              rules.NumLocked = false;

              rules.Extra = 3;
              rules.ExtraLocked = false;

              rules.CanReceive = CanVoteOrReceive.All;
              break;

            case ElectionMode.TieBreak:
              rules.Num = 1;
              rules.NumLocked = false;

              rules.Extra = 0;
              rules.ExtraLocked = true;

              rules.CanReceive = CanVoteOrReceive.NamedPeople;
              break;

            case ElectionMode.ByElection:
              throw new ApplicationException("Unit Conventions cannot have by-elections");
          }
          rules.CanReceiveLocked = true;
          break;

        case "Reg":
          rules.CanVote = CanVoteOrReceive.NamedPeople; // LSA members
          rules.CanVoteLocked = false;

          switch (mode)
          {
            case ElectionMode.Normal:
              rules.Num = 9;
              rules.NumLocked = false;

              rules.Extra = 3;
              rules.ExtraLocked = false;

              rules.CanReceive = CanVoteOrReceive.All;
              break;

            case ElectionMode.TieBreak:
              rules.Num = 1;
              rules.NumLocked = false;

              rules.Extra = 0;
              rules.ExtraLocked = true;

              rules.CanReceive = CanVoteOrReceive.NamedPeople;
              break;

            case ElectionMode.ByElection:
              // Regional Councils often do not have by-elections, but some countries may allow it?

              rules.Num = 1;
              rules.NumLocked = false;

              rules.Extra = 0;
              rules.ExtraLocked = true;

              rules.CanReceive = CanVoteOrReceive.All;
              break;
          }
          rules.CanReceiveLocked = true;
          break;

        case "Oth":
          rules.CanVote = CanVoteOrReceive.All;

          rules.CanVoteLocked = false;
          rules.CanReceiveLocked = false;
          rules.NumLocked = false;
          rules.ExtraLocked = false;

          switch (mode)
          {
            case ElectionMode.Normal:
              rules.Num = 9;
              rules.Extra = 0;
              rules.CanReceive = CanVoteOrReceive.All;
              break;

            case ElectionMode.TieBreak:
              rules.Num = 1;
              rules.Extra = 0;
              rules.CanReceive = CanVoteOrReceive.NamedPeople;
              break;

            case ElectionMode.ByElection:
              rules.Num = 1;
              rules.Extra = 0;
              rules.CanReceive = CanVoteOrReceive.All;
              break;
          }
          break;
      }

      return rules;
    }

    /// <summary>
    /// Based on the 2 flags, get a default reason (may be null)
    /// </summary>
    /// <returns></returns>
    public IneligibleReasonEnum GetDefaultIneligibleReason()
    {
      var canVote = UserSession.CurrentElection.CanVote == ElectionModel.CanVoteOrReceive.All;
      var canReceiveVotes = UserSession.CurrentElection.CanReceive == ElectionModel.CanVoteOrReceive.All;
    
      if (canVote && canReceiveVotes)
      {
        return null;
      }
      if (!canVote && !canReceiveVotes)
      {
        return IneligibleReasonEnum.Ineligible_Other;
      }
      if (!canVote)
      {
        return IneligibleReasonEnum.IneligiblePartial2_Not_a_Delegate;
      }
      return IneligibleReasonEnum.IneligiblePartial1_Not_in_TieBreak;
    }


    //    public Election GetFreshFromDb(Guid electionGuid)
    //    {
    //      return Election.ThisElectionCached;// Db.Election.FirstOrDefault(e => e.ElectionGuid == electionGuid);
    //    }
    /// <Summary>Gets directly from the database, not session. Stores in session.</Summary>
    /// <Summary>Saves changes to this election</Summary>
    public JsonResult SaveElection(Election electionFromBrowser)
    {
      //      var election = Db.Election.SingleOrDefault(e => e.C_RowId == electionFromBrowser.C_RowId);
      var electionCacher = new ElectionCacher();

      var election = UserSession.CurrentElection;
      Db.Election.Attach(election);

      var currentType = election.ElectionType;
      var currentMode = election.ElectionMode;
      var currentCan = election.CanVote;
      var currentReceive = election.CanReceive;

      // List of fields to allow edit from setup page
      var editableFields = new
      {
        election.Name,
        election.DateOfElection,
        election.Convenor,
        election.ElectionType,
        election.ElectionMode,
        election.NumberToElect,
        election.NumberExtra,
        election.CanVote,
        election.CanReceive,
        election.ListForPublic,
        election.ShowAsTest,
        election.ElectionPasscode,
        election.UseCallInButton,
        election.HidePreBallotPages,
        election.MaskVotingMethod
      }.GetAllPropertyInfos().Select(pi => pi.Name).ToArray();


      var changed = electionFromBrowser.CopyPropertyValuesTo(election, editableFields);

      if (changed)
      {
        Db.SaveChanges();

        electionCacher.UpdateItemAndSaveCache(election);

        new PublicHub().ElectionsListUpdated(); // in case the name, or ListForPublic, etc. has changed
      }

      if (currentMode != election.ElectionMode
          || currentType != election.ElectionType
          || currentCan != election.CanVote
          || currentReceive != election.CanReceive
        )
      {
        // reset flags
        new PeopleModel().SetInvolvementFlagsToDefault();

        // update analysis
        new ResultsModel().GenerateResults();
      }

      var displayName = UserSession.CurrentElectionDisplayNameAndInfo;

      return new
      {
        Status = "Saved",
        // TODO 2011-11-20 Glen Little: Return entire election?
        Election = election,
        displayName
      }.AsJsonResult();
    }

    public bool JoinIntoElection(Guid wantedElectionGuid)
    {
      // don't use cache, go directly to database - cache is tied to current election
      var exists = Db.Election.Any(e => e.ElectionGuid == wantedElectionGuid);
      if (!exists)
      {
        return false;
      }

      if (UserSession.CurrentElectionGuid == wantedElectionGuid)
      {
        return true;
      }

      // switch this the whole environment to use this election
      var computer = UserSession.CurrentComputer;
      if (computer != null)
      {
        new ComputerCacher().RemoveItemAndSaveCache(computer);
      }
      
      UserSession.ResetWhenSwitchingElections();

      UserSession.CurrentElectionGuid = wantedElectionGuid;

      var message = UserSession.IsGuestTeller
        ? "Guest teller joined into Election"
        : "Teller (" + UserSession.MemberName + ") switched into Election";

      new LogHelper().Add(message);

      return true;
    }

    //public JsonResult Copy(Guid guidOfElectionToCopy)
    //{
    //    if (UserSession.IsGuestTeller)
    //    {
    //        return new
    //                 {
    //                     Success = false,
    //                     Message = "Not authorized"
    //                 }.AsJsonResult();
    //    }

    //    var election = Db.Election.SingleOrDefault(e => e.ElectionGuid == guidOfElectionToCopy);
    //    if (election == null)
    //    {
    //        return new
    //                 {
    //                     Success = false,
    //                     Message = "Not found"
    //                 }.AsJsonResult();
    //    }

    //    // copy in SQL
    //    var result = Db.CloneElection(election.ElectionGuid, UserSession.LoginId).SingleOrDefault();
    //    if (result == null)
    //    {
    //        return new
    //                 {
    //                     Success = false,
    //                     Message = "Unable to copy"
    //                 }.AsJsonResult();
    //    }
    //    if (!result.Success.AsBoolean())
    //    {
    //        return new
    //                 {
    //                     Success = false,
    //                     Message = "Sorry: " + result.Message
    //                 }.AsJsonResult();
    //    }
    //    election = Db.Election.SingleOrDefault(e => e.ElectionGuid == result.NewElectionGuid);
    //    if (election == null)
    //    {
    //        return new
    //                 {
    //                     Success = false,
    //                     Message = "New election not found"
    //                 }.AsJsonResult();
    //    }
    //    UserSession.CurrentElection = election;
    //    return new
    //             {
    //                 Success = true,
    //                 election.ElectionGuid
    //             }.AsJsonResult();
    //}

    public JsonResult Create()
    {
      if (UserSession.IsGuestTeller)
      {
        return new
        {
          Success = false,
          Message = "Not authorized"
        }.AsJsonResult();
      }

      // create an election for this ID
      // create a default Location
      // assign all of these to this person and computer

      UserSession.ResetWhenSwitchingElections();

      var election = new Election
      {
        Convenor = "[Convenor]",
        ElectionGuid = Guid.NewGuid(),
        Name = "[New Election]",
        ElectionType = "LSA",
        ElectionMode = ElectionMode.Normal,
        TallyStatus = ElectionTallyStatusEnum.NotStarted,
        NumberToElect = 9,
        NumberExtra = 0,
        CanVote = CanVoteOrReceive.All,
        CanReceive = CanVoteOrReceive.All
      };

      Db.Election.Add(election);
      Db.SaveChanges();

      UserSession.CurrentElectionGuid = election.ElectionGuid;

      //      new ElectionStatusSharer().SetStateFor(election);

      var join = new JoinElectionUser
      {
        ElectionGuid = election.ElectionGuid,
        UserId = UserSession.UserGuid
      };
      Db.JoinElectionUser.Add(join);


      var mainLocation = new Location
      {
        Name = "Main Location",
        LocationGuid = Guid.NewGuid(),
        ElectionGuid = election.ElectionGuid,
        SortOrder = 1
      };
      Db.Location.Add(mainLocation);
      Db.SaveChanges();
      new LocationCacher().UpdateItemAndSaveCache(mainLocation);

//      var computerModel = new ComputerModel();
//      computerModel.CreateComputerForMe();

      return new
      {
        Success = true
      }.AsJsonResult();
    }

    public void SetTallyStatus(Controller controller, string status)
    {
      if (UserSession.IsGuestTeller)
      {
        return;
      }

      var electionCacher = new ElectionCacher();
      var election = UserSession.CurrentElection;
      if (election.TallyStatus != status)
      {
        Db.Election.Attach(election);

        election.TallyStatus = status;

        Db.SaveChanges();

        electionCacher.UpdateItemAndSaveCache(election);

        //new ElectionStatusSharer().SetStateFor(election);
//        var menuHelper = new MenuHelper(controller.Url);
        var info = new
        {
//          QuickLinks = menuHelper.QuickLinks(),
//          Name = UserSession.CurrentElectionStatusName,
          StateName = UserSession.CurrentElectionStatus,
        };
//
//        // should always be true... but usage could change in future
//        var currentIsKnown = UserSession.IsKnownTeller;
//        UserSession.IsKnownTeller = false;
////        menuHelper = new MenuHelper(controller.Url);
//        var infoForGuest = new
//        {
////          QuickLinks = menuHelper.QuickLinks(),
////          QuickSelector = menuHelper.StateSelectorItems().ToString(),
////          Name = UserSession.CurrentElectionStatusName,
//          State = UserSession.CurrentElectionStatus,
//        };
//        UserSession.IsKnownTeller = currentIsKnown;

        new MainHub().StatusChanged(info, info);
      }
    }

    //    public IEnumerable<Election> VisibleElections()
    //    {
    //      // this is first hit on the database on the home page... need special logging
    //      try
    //      {
    //        var electionsWithCode =
    //          Db.Election.Where(e => e.ElectionPasscode != null && e.ElectionPasscode != "").ToList();
    //        return
    //          electionsWithCode.Where(
    //            e => e.ListForPublic.AsBoolean() && DateTime.Now - e.ListedForPublicAsOf <= 5.minutes());
    //      }
    //      catch (Exception e)
    //      {
    //        var logger = LogManager.GetCurrentClassLogger();
    //        logger.ErrorException("Reading VisibleElections", e);
    //
    //        return new List<Election>();
    //      }
    //    }

    public JsonResult SetTallyStatusJson(Controller controller, string status)
    {
      SetTallyStatus(controller, status);

      new LogHelper().Add("Status changed to " + status);

      return new
      {
        // QuickLinks = new MenuHelper(controller.Url).QuickLinks(),
        StateName = UserSession.CurrentElectionStatus
      }.AsJsonResult();
    }

    public JsonResult UpdateElectionShowAllJson(bool showFullReport)
    {
      var electionCacher = new ElectionCacher();

      var election = UserSession.CurrentElection;
      if (election.ShowFullReport != showFullReport)
      {
        Db.Election.Attach(election);

        election.ShowFullReport = showFullReport;

        Db.SaveChanges();

        electionCacher.UpdateItemAndSaveCache(election);
      }

      return new { Saved = true }.AsJsonResult();
    }

    public JsonResult UpdateListOnPageJson(bool listOnPage)
    {
      if (UserSession.IsKnownTeller)
      {
        var electionCacher = new ElectionCacher();

        var election = UserSession.CurrentElection;
        Db.Election.Attach(election);

        election.ListForPublic = listOnPage;

        election.ListedForPublicAsOf = listOnPage ? (DateTime?)DateTime.Now : null;

        Db.SaveChanges();

        electionCacher.UpdateItemAndSaveCache(election);

        new PublicHub().ElectionsListUpdated();

        return new { Saved = true }.AsJsonResult();
      }

      return new { Saved = false }.AsJsonResult();
    }

    /// <Summary>Do any processing for the election</Summary>
    /// <returns> True if the status changed </returns>
    //    public bool ProcessPulse()
    //    {
    //      if (!UserSession.CurrentElectionGuid.HasContent())
    //      {
    //        return false;
    //      }
    //
    //
    //      //      var sharer = new ElectionStatusSharer();
    //      //      var sharedState = sharer.GetStateFor(UserSession.CurrentElectionGuid);
    //      //      var someoneElseChangedTheStatus = sharedState != election.TallyStatus;
    //      //      if (someoneElseChangedTheStatus)
    //      //      {
    //      //        //election = GetFreshFromDb(election.ElectionGuid);
    //      //        sharer.SetStateFor(election);
    //      //      }
    //
    //      UpdateListedForPublicTime();
    //
    //      //      return someoneElseChangedTheStatus;
    //      return true;
    //    }

    /// <summary>
    /// Should be called whenever the known teller has contacted us
    /// </summary>
    //    public static void UpdateListedForPublicTime()
    //    {
    //      if (!UserSession.IsKnownTeller)
    //      {
    //        return;
    //      }
    //
    //      var election = UserSession.CurrentElection;
    //      if (election == null || !election.ListForPublic.AsBoolean())
    //      {
    //        return;
    //      }
    //
    //      // don't bother saving this to the database
    //      var now = DateTime.Now;
    //
    //      if (now - election.ListedForPublicAsOf < 1.minutes())
    //      {
    //        // don't need to update in less than a minute
    //        return;
    //      }
    //
    //      election.ListedForPublicAsOf = now;
    //
    //      var electionCacher = new ElectionCacher();
    //      electionCacher.UpdateItemAndSaveCache(election);
    //    }

    #region Nested type: CanVoteOrReceive

    public static class CanVoteOrReceive
    {
      public const string All = "A";
      public const string NamedPeople = "N";
    }

    #endregion

    #region Nested type: ElectionMode

    public static class ElectionMode
    {
      public const string Normal = "N";
      public const string TieBreak = "T";
      public const string ByElection = "B";
    }

    #endregion

    public bool GuestsAllowed()
    {
      return UserSession.CurrentElection != null && UserSession.CurrentElection.ListForPublicNow;
    }
  }
}