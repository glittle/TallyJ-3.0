using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using System.Web.Mvc;
using CsQuery.ExtensionMethods;
using Newtonsoft.Json;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Helper;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ElectionModel : DataConnectedModel
  {
    private static object LockObject = new object();

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

              rules.Extra = 7;
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
    ///   Based on the 2 flags, get a default reason (may be null)
    /// </summary>
    /// <returns></returns>
    public IneligibleReasonEnum GetDefaultIneligibleReason()
    {
      return GetDefaultIneligibleReason(UserSession.CurrentElection);
    }

    /// <summary>
    ///   Based on the 2 flags, get a default reason (may be null)
    /// </summary>
    /// <returns></returns>
    public static IneligibleReasonEnum GetDefaultIneligibleReason(Election election)
    {
      if (election == null)
      {
        return null;
      }

      var canVote = election.CanVote == CanVoteOrReceive.All;
      var canReceiveVotes = election.CanReceive == CanVoteOrReceive.All;

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

    public JsonResult SaveNotification(string emailText)
    {
      if (emailText.HasNoContent())
      {
        return new
        {
          success = false,
          Status = "Cannot have empty email text."
        }.AsJsonResult();
      }

      var electionCacher = new ElectionCacher(Db);
      var election = UserSession.CurrentElection;
      Db.Election.Attach(election);

      election.EmailText = Uri.UnescapeDataString(emailText);

      Db.SaveChanges();

      electionCacher.UpdateItemAndSaveCache(election);
      
      return new
      {
        success = true,
        Status = "Saved",
        defaultFromAddress = UserSession.CurrentElection.EmailFromAddressWithDefault,
      }.AsJsonResult();
    }

    /// <Summary>Gets directly from the database, not session. Stores in session.</Summary>
    /// <Summary>Saves changes to this election</Summary>
    public JsonResult SaveElection(Election electionFromBrowser)
    {
      var electionCacher = new ElectionCacher(Db);

      var election = UserSession.CurrentElection;
      Db.Election.Attach(election);

      var beforeChanges = election.GetAllProperties();

      var currentType = election.ElectionType;
      var currentMode = election.ElectionMode;
      var currentCan = election.CanVote;
      var currentReceive = election.CanReceive;
      var currentListed = election.ListForPublic;

    

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
        election.MaskVotingMethod,
        election.BallotProcessRaw,
        election.EnvNumModeRaw,
        election.T24,
        election.OnlineWhenOpen,
        election.OnlineWhenClose,
        election.OnlineCloseIsEstimate,
        election.OnlineSelectionProcess,
        election.EmailFromAddress,
        election.EmailFromName,
      }.GetAllPropertyInfos().Select(pi => pi.Name).ToArray();

      if (!currentListed.AsBoolean() && election.ListForPublic.AsBoolean())
      {
        // just turned on
        election.ListedForPublicAsOf = DateTime.Now;
      }

      var changed = electionFromBrowser.CopyPropertyValuesTo(election, editableFields);

      var coreSettingsChanged = currentMode != election.ElectionMode
                                || currentType != election.ElectionType
                                || currentCan != election.CanVote
                                || currentReceive != election.CanReceive;

      if (coreSettingsChanged)
      {
        var setupModel = new SetupModel();
        if (setupModel.HasBallots || setupModel.HasOnlineBallots)
        {
          return new
          {
            success = false,
            Status = "Cannot change type of election after ballots are entered."
          }.AsJsonResult();
        }
      }

      var locationModel = new LocationModel();
      var onlineLocation = locationModel.GetOnlineLocation();

      if (changed)
      {
        // check if trying to turn off online elections
        if (!election.OnlineEnabled)
        {
          if (onlineLocation != null)
          {
            if (locationModel.IsLocationInUse(onlineLocation.LocationGuid))
            {
              beforeChanges.CopyPropertyValuesTo(election);

              // we have ballots from Online... can't remove Open date!
              return new
              {
                success = false,
                Status = "Online ballots received. Cannot disable online voting.",
                Election = election,
                displayName = UserSession.CurrentElectionDisplayNameAndInfo
              }.AsJsonResult();
            }
          }
        }

        Db.SaveChanges();

        electionCacher.UpdateItemAndSaveCache(election);

        new PublicHub()
            .TellPublicAboutVisibleElections(); // in case the name, or ListForPublic, etc. has changed
      }


      if (coreSettingsChanged)
      {
        // reset flags
        new PeopleModel().SetInvolvementFlagsToDefault();

        // update analysis
        new ResultsModel().GenerateResults();
      }

      // adjust for Online
      if (election.OnlineEnabled)
      {
        if (onlineLocation == null)
        {
          // need a new location for online!
          locationModel.EditLocation(0, LocationModel.OnlineLocationName, true);
        }
      }
      else
      {
        // not enabled
        if (onlineLocation != null)
        {
          // remove it (already checked that it is not in use)
          locationModel.EditLocation(onlineLocation.C_RowId, "", true);
        }
      }

      new AllVotersHub()
        .UpdateVoters(new
        {
          changed = true,
          election.OnlineWhenClose,
          election.OnlineWhenOpen,
          election.OnlineCloseIsEstimate,
          election.OnlineSelectionProcess
        });
      new FrontDeskHub()
        .UpdateOnlineElection(new
        {
          election.OnlineWhenClose,
          election.OnlineWhenOpen,
          election.OnlineCloseIsEstimate,
        });

      // alert will go out when the scheduled job runs

      return new
      {
        success = true,
        Status = "Saved",
        defaultFromAddress = UserSession.CurrentElection.EmailFromAddressWithDefault,
        Election = election,
        displayName = UserSession.CurrentElectionDisplayNameAndInfo
      }.AsJsonResult();
    }

    //    private void SaveOnlineElection(OnlineElection onlineElectionFromBrowser, bool useOnline)
    //    {
    //      // save online election - ignore Guid from browser
    //      var onlineInDb = Db.OnlineElection.FirstOrDefault(oe => oe.ElectionGuid == UserSession.CurrentElectionGuid);
    //      if (useOnline)
    //      {
    //        if (onlineInDb == null)
    //        {
    //          onlineInDb = new OnlineElection
    //          {
    //            ElectionGuid = UserSession.CurrentElectionGuid,
    //            ElectionName = UserSession.CurrentElectionName,
    //          };
    //          Db.OnlineElection.Add(onlineInDb);
    //        }
    //
    //        var changed = onlineElectionFromBrowser.CopyPropertyValuesTo(onlineInDb, new
    //        {
    //          onlineInDb.CloseIsEstimate,
    //          onlineInDb.WhenOpen,
    //          onlineInDb.WhenClose,
    //        }.GetAllPropertyInfos().Select(pi => pi.Name).ToArray());
    //
    //        if (changed)
    //        {
    //          Db.SaveChanges();
    //
    //          new AllVotersHub()
    //            .UpdateVoters(new
    //            {
    //              changed = true,
    //              onlineInDb.WhenClose,
    //              onlineInDb.WhenOpen,
    //              onlineInDb.CloseIsEstimate,
    //            });
    //        }
    //        UserSession.UsingOnlineElection = true;
    //      }
    //      else
    //      {
    //        if (onlineInDb != null)
    //        {
    //          Db.OnlineElection.Remove(onlineInDb);
    //          Db.SaveChanges();
    //        }
    //        UserSession.UsingOnlineElection = false;
    //      }
    //
    //      new AllVotersHub().UpdateVoters(new { changed = true });
    //
    //    }

    //    public bool JoinIntoElection(int wantedElectionId, Guid oldComputerGuid)
    //    {
    //      var electionGuid = Db.Election.Where(e => e.C_RowId == wantedElectionId).Select(e => e.ElectionGuid)
    //          .FirstOrDefault();
    //      if (electionGuid == Guid.Empty)
    //      {
    //        return false;
    //      }
    //
    //      return JoinIntoElection(electionGuid, oldComputerGuid);
    //    }

    public bool JoinIntoElection(Guid wantedElectionGuid, Guid oldComputerGuid)
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
      UserSession.LeaveElection(true);

      // move into new election
      UserSession.CurrentElectionGuid = wantedElectionGuid;

      // assign new computer code
      var computerModel = new ComputerModel();
      computerModel.GetComputerForMe(oldComputerGuid);

      string message;
      if (UserSession.IsGuestTeller)
      {
        message = "Guest teller joined into Election";
      }
      else
      {
        message = "Teller (" + UserSession.MemberName + ") switched into Election";

        new PublicHub().TellPublicAboutVisibleElections();

        UpgradeOldData();
      }

      new LogHelper().Add("{0} (Comp {1})".FilledWith(message, UserSession.CurrentComputerCode), true);

      return true;
    }

    private void UpgradeOldData()
    {
      var personCacher = new PersonCacher(Db);
      var testInfo = personCacher.AllForThisElection.Select(p => new { p.CombinedInfo }).FirstOrDefault();

      if (testInfo == null)
      {
        return;
      }

      if (testInfo.CombinedInfo.HasContent() && !testInfo.CombinedInfo.Contains("^"))
      {
        return;
      }

      // fix all data
      var voteCacher = new VoteCacher(Db);

      var people = personCacher.AllForThisElection;
      var votes = voteCacher.AllForThisElection;

      var peopleModel = new PeopleModel();
      var saveNeeded = false;

      foreach (var person in people)
      {
        AutoFix(person, votes, peopleModel, ref saveNeeded);
      }

      if (saveNeeded)
      {
        Db.SaveChanges();

        new LogHelper().Add("Updated person combined infos");
      }

      personCacher.DropThisCache();
      voteCacher.DropThisCache();
    }

    public void AutoFix(Person person, List<Vote> voteList, PeopleModel peopleModel, ref bool saveNeeded)
    {
      var oldCombined = person.CombinedInfo;

      peopleModel.SetCombinedInfos(person);

      if (person.CombinedInfo == oldCombined)
      {
        //didn't need to fix it
        return;
      }

      saveNeeded = true;

      foreach (var vote in voteList.Where(v => v.PersonGuid == person.PersonGuid))
      {
        vote.PersonCombinedInfo = person.CombinedInfo;
      }
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
        Convenor = "[Convener]", // correct spelling is Convener. DB field name is wrong.
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
      new LocationCacher(Db).UpdateItemAndSaveCache(mainLocation);

      //      var computerModel = new ComputerModel();
      //      computerModel.CreateComputerForMe();

      return new
      {
        Success = true
      }.AsJsonResult();
    }

    public void SetTallyStatus(string status)
    {
      if (UserSession.IsGuestTeller)
      {
        return;
      }

      var electionCacher = new ElectionCacher(Db);
      var election = UserSession.CurrentElection;

      if (election.TallyStatus != status)
      {
        Db.Election.Attach(election);

        election.TallyStatus = status;

        Db.SaveChanges();

        electionCacher.UpdateItemAndSaveCache(election);

        UpdateStatusInBrowsers();
      }
    }

    public static void UpdateStatusInBrowsers()
    {
      var info = new
      {
        StateName = UserSession.CurrentElectionStatus,
        Online = UserSession.CurrentElection.OnlineCurrentlyOpen,
        Passcode = UserSession.CurrentElection.ElectionPasscode,
        Listed = UserSession.CurrentElection.ListedForPublicAsOf != null
      };

      new MainHub().StatusChanged(info, info);
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

    public JsonResult SetTallyStatusJson(string status)
    {
      var summary =
          new ResultSummaryCacher(Db).AllForThisElection.SingleOrDefault(rs => rs.ResultType == ResultType.Final);
      var readyForReports = summary != null && summary.UseOnReports.AsBoolean();
      if (status == ElectionTallyStatusEnum.Finalized && !readyForReports)
      {
        return new
        {
          Message = "Cannot set to \"Finalized\" until Analysis is completed successfully."
        }.AsJsonResult();
      }

      SetTallyStatus(status);

      new LogHelper().Add("Status changed to " + status, true);

      return new
      {
        StateName = UserSession.CurrentElectionStatus
      }.AsJsonResult();
    }

    internal int GetNextEnvelopeNumber()
    {
      // create a new env number - Jan 2018 - create number for each ballot. it may be displayed or not

      // do we need a transaction here to ensure no duplicates are made?

      int nextNum;

      lock (LockObject)
      {
        var election = UserSession.CurrentElection;
        Db.Election.Attach(election);

        nextNum = election.LastEnvNum.AsInt() + 1;
        election.LastEnvNum = nextNum;

        new ElectionCacher(Db).UpdateItemAndSaveCache(election);
      }

      Db.SaveChanges(); // save immediately, may include person saving

      return nextNum;
    }

    public JsonResult UpdateElectionShowAllJson(bool showFullReport)
    {
      var electionCacher = new ElectionCacher(Db);

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
        var electionCacher = new ElectionCacher(Db);

        var election = UserSession.CurrentElection;
        Db.Election.Attach(election);

        election.ListForPublic = listOnPage;
        election.ListedForPublicAsOf = listOnPage ? (DateTime?)DateTime.Now : null;

        Db.SaveChanges();

        electionCacher.UpdateItemAndSaveCache(election);

        new PublicHub().TellPublicAboutVisibleElections();

        UpdateStatusInBrowsers();

        return new { Saved = true }.AsJsonResult();
      }

      return new { Saved = false }.AsJsonResult();
    }

    public bool GuestsAllowed()
    {
      var election = UserSession.CurrentElection;
      return election != null && election.CanBeAvailableForGuestTellers;
    }

    /// <summary>
    ///   Closes the election, logging out all guests.
    ///   If another known teller is still logged in, they will need to access the server to open the election again.
    /// </summary>
    public void CloseElection()
    {
      var election = UserSession.CurrentElection;

      if (election != null)
      {
        if (Db.Election.Local.All(e => e.ElectionGuid != election.ElectionGuid))
        {
          Db.Election.Attach(election);
        }

        election.ListedForPublicAsOf = null;

        Db.SaveChanges();

        new MainHub().CloseOutGuestTellers();

        new BallotCacher().DropThisCache();
        //new ComputerCacher().DropThisCache();
        new ElectionCacher().DropThisCache();
        new LocationCacher().DropThisCache();
        new PersonCacher().DropThisCache();
        new ResultCacher().DropThisCache();
        new ResultSummaryCacher().DropThisCache();
        new ResultTieCacher().DropThisCache();
        new TellerCacher().DropThisCache();
        new VoteCacher().DropThisCache();
      }

      new PublicHub().TellPublicAboutVisibleElections();
    }

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
    /// <Summary>Do any processing for the election</Summary>
    /// <returns> True if the status changed </returns>
    /// <summary>
    ///   Should be called whenever the known teller has contacted us
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
    //      var electionCacher = new ElectionCacher(Db);
    //      electionCacher.UpdateItemAndSaveCache(election);
    //    }

    //public void UpdateElectionWhenComputerFreshnessChanges(List<Computer> computers = null)
    //{
    //  var currentElection = UserSession.CurrentElection;
    //  if (currentElection == null)
    //  {
    //    return;
    //  }

    //  var lastContactOfTeller = (computers ?? new ComputerCacher().AllForThisElection)
    //    .Where(c => c.AuthLevel == "Known")
    //    .Max(c => c.LastContact);

    //  if (lastContactOfTeller != null &&
    //      (currentElection.ListedForPublicAsOf == null
    //       ||
    //       Math.Abs((DateTime.Now - lastContactOfTeller.Value).TotalMinutes) >
    //       5.minutes().TotalMinutes))
    //  {
    //    currentElection.ListedForPublicAsOf = lastContactOfTeller;
    //    new ElectionCacher(Db).UpdateItemAndSaveCache(currentElection);
    //  }

    //  //new PublicElectionLister().UpdateThisElectionInList();
    //  new PublicHub().TellPublicAboutVisibleElections();

    //}
    public static class CanVoteOrReceive
    {
      public const string All = "A";
      public const string NamedPeople = "N";
    }

    public static class ElectionMode
    {
      public const string Normal = "N";
      public const string TieBreak = "T";
      public const string ByElection = "B";
    }

    //    public JsonResult CloseOnline(int minutes, bool est)
    //    {
    //      var electionCacher = new ElectionCacher(Db);
    //
    //      var election = UserSession.CurrentElection;
    //      Db.Election.Attach(election);
    //
    //      election.OnlineWhenClose = minutes == 0
    //        ? DateTime.Now.ChopToMinute()
    //        : DateTime.Now.ChopToMinute().AddMinutes(minutes);
    //      election.OnlineCloseIsEstimate = est;
    //
    //      Db.SaveChanges();
    //
    //      electionCacher.UpdateItemAndSaveCache(election);
    //
    //      new AllVotersHub()
    //        .UpdateVoters(new
    //        {
    //          election.OnlineWhenClose,
    //          election.OnlineWhenOpen,
    //          election.OnlineCloseIsEstimate,
    //        });
    //
    //      return new
    //      {
    //        success = true,
    //        election.OnlineWhenClose,
    //        election.OnlineCloseIsEstimate,
    //      }.AsJsonResult();
    //    }

    public JsonResult ProcessOnlineBallots()
    {
      // only process when election is closed
      // process in "random" order (not related to when they submitted)
      // for each voter, verify their status 
      // create ballot
      // change status to Processed

      var now = DateTime.Now;
      var election = UserSession.CurrentElection;
      var numToElect = election.NumberToElect.AsInt(0);

      if (numToElect < 1)
      {
        return new
        {
          Message = "Election has invalid number to elect."
        }.AsJsonResult();
      }

      if (election.TallyStatus == ElectionTallyStatusEnum.Finalized)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }

      if (election.OnlineCurrentlyOpen)
      {
        return new
        {
          Message = "Cannot process online ballots while election is open."
        }.AsJsonResult();
      }

      // ensure it has a close time in the past
      if (!election.OnlineWhenClose.HasValue || election.OnlineWhenClose.Value > now)
      {
        return new
        {
          Message = "Cannot process online ballots when the Close time is not valid."
        }.AsJsonResult();
      }

      var electionGuid = election.ElectionGuid;

      // checking ElectionGuid for person is redundant but doesn't hurt
      // checking Ready and PoolLocked is redundant but doesn't hurt
      var ballotInfoList = Db.OnlineVotingInfo
        .Where(ovi => ovi.ElectionGuid == electionGuid)
        .Where(ovi => ovi.Status == OnlineBallotStatusEnum.Submitted && ovi.PoolLocked.Value)
        .Join(Db.Person.Where(p => p.ElectionGuid == electionGuid && p.VotingMethod == VotingMethodEnum.Online), ovi => ovi.PersonGuid, p => p.PersonGuid, (ovi, p) => new { ovi, p })
        // .Join(Db.OnlineVoter, j => new { j.ovi.Email, j.ovi.Phone }, ov => new { ov.Email, ov.Phone }, (j, ov) => new { j.p, j.ovi, ov })
        // .OrderBy(j => j.ovi.PersonGuid) -- no defined order... will resort later
        .ToList();

      if (!ballotInfoList.Any())
      {
        return new
        {
          success = true,
          Message = "No online ballots are Submitted and marked as voting Online."
        }.AsJsonResult();
      }

      var voterIdList = ballotInfoList
        .Where(b => b.p.Email.HasContent())
        .Select(b => new { VoterId = b.p.Email, b.p.PersonGuid })
        .Concat(ballotInfoList
          .Where(b => b.p.Phone.HasContent())
          .Select(b => new { VoterId = b.p.Phone, b.p.PersonGuid }))
        .GroupJoin(Db.OnlineVoter, v => v.VoterId, ov => ov.VoterId, (v, ovList) => new { v.PersonGuid, ovList })
        .GroupBy(j => j.PersonGuid)
        .ToDictionary(g => g.Key, j => j.SelectMany(o => o.ovList));

      var ballotModel = BallotModelFactory.GetForCurrentElection();
      var problems = new List<string>();
      var numBallotsCreated = 0;
      var emailHelper = new EmailHelper();
      var smsHelper = new SmsHelper();
      var logHelper = new LogHelper();

      foreach (var onlineBallotInfo in ballotInfoList)
      {
        var name = " - " + onlineBallotInfo.p.FullName;

        if (!onlineBallotInfo.p.CanVote.GetValueOrDefault())
        {
          problems.Add("Not allowed to vote" + name);
          continue;
        }

        var pool = onlineBallotInfo.ovi.ListPool;
        if (pool.HasNoContent())
        {
          problems.Add("Empty pool" + name);
          continue;
        }

        var completePool = JsonConvert.DeserializeObject<List<OnlineRawVote>>(pool);

        var poolList = completePool.Take(numToElect).ToList();
        if (poolList.Count != numToElect)
        {
          problems.Add($"Pool too small ({completePool.Count})" + name);
          continue;
        }

        var ballotCreated = false;


        using (var transaction = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromSeconds(Debugger.IsAttached ? 600 : 30)))
        {
          try
          {
            ballotCreated = ballotModel.CreateBallotForOnlineVoter(poolList, out var message);
            if (ballotCreated)
            {
              onlineBallotInfo.ovi.Status = OnlineBallotStatusEnum.Processed;
              onlineBallotInfo.ovi.WhenStatus = now;
              onlineBallotInfo.ovi.WhenBallotCreated = now;
              onlineBallotInfo.ovi.HistoryStatus += ";{0}|{1}".FilledWith(onlineBallotInfo.ovi.Status, now.ToJSON());

              onlineBallotInfo.ovi.ListPool = null; // ballot created, so wipe out the original list
              onlineBallotInfo.ovi.PoolLocked = null;

              Db.SaveChanges();

              if (onlineBallotInfo.p.Email.HasContent())
              {
                logHelper.Add("Ballot processed", false, onlineBallotInfo.p.Email);
              }
              if (onlineBallotInfo.p.Phone.HasContent())
              {
                logHelper.Add("Ballot processed", false, onlineBallotInfo.p.Phone);
              }

              transaction.Complete();

              numBallotsCreated++;

              new VoterPersonalHub().Update(onlineBallotInfo.p);
            }
            else
            {
              problems.Add(message + name);
            }
          }
          catch (Exception e)
          {
            problems.Add($"Error: {e.LastException().Message}{name}");
          }
        }

        Db.SaveChanges(); // redundant?

        if (ballotCreated)
        {
          // keep this outside the transaction
          var personGuid = onlineBallotInfo.p.PersonGuid;
          if (voterIdList.ContainsKey(personGuid))
          {
            foreach (var onlineVoter in voterIdList[personGuid])
            {
              if (onlineVoter.VoterIdType == VoterIdTypeEnum.Email)
              {
                emailHelper.SendWhenProcessed(UserSession.CurrentElection, onlineBallotInfo.p, onlineVoter, logHelper, out var emailError);
                if (emailError.HasContent())
                {
                  problems.Add($"Error: {emailError}");
                }
              }

              if (onlineVoter.VoterIdType == VoterIdTypeEnum.Phone)
              {
                smsHelper.SendWhenProcessed(UserSession.CurrentElection, onlineBallotInfo.p, onlineVoter, logHelper, out var smsError);
                if (smsError.HasContent())
                {
                  problems.Add($"Error: {smsError}");
                }
              }
            }
          }
        }
      }

      // all ballots done
      var onlineBallots = Db.Ballot
        .Join(Db.Location.Where(l => l.ElectionGuid == electionGuid), b => b.LocationGuid, l => l.LocationGuid, (b, l) => b)
        .Where(b => b.ComputerCode == ComputerModel.ComputerCodeForOnline)
        .ToList();

      var rnd = new Random();
      var sorted = onlineBallots.OrderBy(b => rnd.Next(0, 99999)).ToList();

      var ballotNum = 1;
      sorted.ForEach(b =>
      {
        b.BallotNumAtComputer = ballotNum++;
      });
      Db.SaveChanges();
      new BallotCacher().DropThisCache();


      return new
      {
        success = numBallotsCreated > 0,
        problems
      }.AsJsonResult();
    }

    public JsonResult SaveOnlineClose(DateTime when, bool est)
    {
      var electionCacher = new ElectionCacher(Db);

      var election = UserSession.CurrentElection;
      Db.Election.Attach(election);

      election.OnlineWhenClose = when;
      election.OnlineCloseIsEstimate = est;

      var sendEmail = false;
      if (election.OnlineCurrentlyOpen)
      {
        election.OnlineAnnounced = null;
        sendEmail = true;
      }

      Db.SaveChanges();

      electionCacher.UpdateItemAndSaveCache(election);

      new AllVotersHub()
        .UpdateVoters(new
        {
          election.OnlineWhenClose,
          election.OnlineWhenOpen,
          election.OnlineCloseIsEstimate,
        });
      new FrontDeskHub()
        .UpdateOnlineElection(new
        {
          election.OnlineWhenClose,
          election.OnlineWhenOpen,
          election.OnlineCloseIsEstimate,
        });

      UpdateStatusInBrowsers();

      // string emailResult = null;
      // if (sendEmail)
      // {
      //   emailResult = new EmailHelper().SendWhenOpened(election);
      // }

      return new
      {
        success = true,
        // emailResult,
        election.OnlineWhenClose,
        election.OnlineCloseIsEstimate,
      }.AsJsonResult();
    }
  }
}