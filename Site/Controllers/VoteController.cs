using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
// using CsQuery.ExtensionMethods;
using Newtonsoft.Json;
using RazorEngine.Compilation.ImpromptuInterface;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  [AllowVoter]
  public class VoteController : BaseController
  {
    public ActionResult Index()
    {
      return View("VoteHome");
    }

    public void JoinVoterHubs(string connId)
    {
      //      try
      //      {
      new AllVotersHub().Join(connId);
      new VoterPersonalHub().Join(connId);
      //        return new { success = true }.AsJsonResult();
      //      }
      //      catch (Exception e)
      //      {
      //        return new { success = false, e.Message }.AsJsonResult();
      //      }
    }

    public JsonResult JoinElection(Guid electionGuid, Guid peopleElectionGuid)
    {
      var voterId = UserSession.VoterId;
      if (voterId.HasNoContent())
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      // for two-stage elections, this will be the 'main' election guid

      // confirm that this person is in the election
      var personQuery = Db.Person.Where(p => p.ElectionGuid == peopleElectionGuid);

      if (UserSession.VoterIdType == VoterIdTypeEnum.Email)
      {
        personQuery = personQuery.Where(p => p.Email == voterId);
      }
      else if (UserSession.VoterIdType == VoterIdTypeEnum.Phone)
      {
        personQuery = personQuery.Where(p => p.Phone == voterId);
      }
      else if (UserSession.VoterIdType == VoterIdTypeEnum.Kiosk)
      {
        personQuery = personQuery.Where(p => p.Voter.KioskCode == voterId);
      }
      else
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var electionPersonInfo = personQuery
        .Join(Db.Election.Where(e => e.ElectionGuid == electionGuid),
          p => p.ElectionGuid,
          e => e.PeopleElectionGuid,
          (p, e) => new { e, p })
        .SingleOrDefault();

      if (electionPersonInfo == null)
      {
        return new
        {
          Error = "Invalid election"
        }.AsJsonResult();
      }

      var utcNow = DateTime.UtcNow;

      // get voting info
      var votingInfo = Db.OnlineVotingInfo
        .SingleOrDefault(ovi => ovi.ElectionGuid == electionGuid && ovi.PersonGuid == electionPersonInfo.p.PersonGuid);

      if (electionPersonInfo.e.OnlineWhenOpen.AsUtc() <= utcNow && electionPersonInfo.e.OnlineWhenClose.AsUtc() > utcNow)
      {
        // put election in session
        UserSession.CurrentElectionGuid = electionPersonInfo.e.ElectionGuid;
        UserSession.CurrentPeopleElectionGuid = electionPersonInfo.e.PeopleElectionGuid;
        UserSession.CurrentParentElectionGuid = electionPersonInfo.e.ParentElectionGuid ?? Guid.Empty;

        UserSession.VoterInElectionPersonGuid = electionPersonInfo.p.PersonGuid;
        // UserSession.VoterInElectionPersonName = electionInfo.p.C_FullNameFL;
        string poolDecryptError = null;

        if (votingInfo == null)
        {
          votingInfo = new OnlineVotingInfo
          {
            ElectionGuid = electionPersonInfo.e.ElectionGuid,
            PersonGuid = electionPersonInfo.p.PersonGuid,
            Status = OnlineBallotStatusEnum.New,
            WhenStatus = utcNow,
            HistoryStatus = "New|{0}".FilledWith(utcNow.ToString("u"))
          };
          Db.OnlineVotingInfo.Add(votingInfo);
          Db.SaveChanges();
        }
        else
        {
          if (EncryptionHelper.IsEncrypted(votingInfo.ListPool))
          {
            votingInfo.ListPool = new OnlineVoteHelper().GetDecryptedListPool(votingInfo, out poolDecryptError);
          }
        }

        // okay
        return new
        {
          open = true,
          voterName = electionPersonInfo.p.C_FullName,
          electionPersonInfo.e.NumberToElect,
          OnlineSelectionProcess = electionPersonInfo.e.OnlineSelectionProcess.DefaultTo(OnlineSelectionProcessEnum.Random.ToString().Substring(0, 1)),
          electionPersonInfo.e.RandomizeVotersList,
          registration = VotingMethodEnum.TextFor(electionPersonInfo.p.Voter.VotingMethod),
          votingInfo,
          poolDecryptError
        }.AsJsonResult();
      }

      return new
      {
        closed = true,
        votingInfo
      }.AsJsonResult();
    }

    public JsonResult LeaveElection()
    {
      // forget election
      UserSession.CurrentContext.Session.Remove(SessionKey.CurrentElectionGuid);
      UserSession.CurrentContext.Session.Remove(SessionKey.VoterInElectionPersonGuid);

      // leave hub?


      return new
      {
        success = true
      }.AsJsonResult();
    }

    public JsonResult SavePool(string pool)
    {
      var electionGuid = UserSession.CurrentElectionGuid;
      var personGuid = UserSession.VoterInElectionPersonGuid;
      var logHelper = new LogHelper();

      var onlineVotingInfo = Db.OnlineVotingInfo
        .SingleOrDefault(ovi => ovi.ElectionGuid == electionGuid && ovi.PersonGuid == personGuid);

      if (onlineVotingInfo == null)
      {
        logHelper.Add("OnlineVotingInfo is null when saving pool", true);
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      if (pool.HasNoContent())
      {
        // shouldn't be possible, but happened
        pool = "[]";
      }

      // var now = DateTime.Now;
      var utcNow = DateTime.UtcNow;
      if (UserSession.CurrentElection.OnlineWhenOpen.AsUtc() <= utcNow
          && UserSession.CurrentElection.OnlineWhenClose.AsUtc() > utcNow)
      {
        // pool is JSON string
        var newStatus = pool == "[]" ? OnlineBallotStatusEnum.New : OnlineBallotStatusEnum.Draft;
        if (newStatus != onlineVotingInfo.Status)
        {
          onlineVotingInfo.Status = newStatus;
        }

        new OnlineVoteHelper().SetListPoolEncrypted(onlineVotingInfo, pool);

        onlineVotingInfo.WhenStatus = utcNow;

        Db.SaveChanges();

        // okay
        return new
        {
          success = true,
          newStatus = newStatus.DisplayText
        }.AsJsonResult();
      }

      return new
      {
        Error = "Closed"
      }.AsJsonResult();
    }

    /// <summary>
    /// Mark online ballot as "submitted" or locked.
    /// </summary>
    /// <param name="locked"></param>
    /// <returns></returns>
    public JsonResult LockPool(bool locked)
    {
      var currentElection = UserSession.CurrentElection;
      var personGuid = UserSession.VoterInElectionPersonGuid;

      var onlineVotingInfo = Db.OnlineVotingInfo
        .SingleOrDefault(ovi => ovi.ElectionGuid == currentElection.ElectionGuid && ovi.PersonGuid == personGuid);

      if (onlineVotingInfo == null)
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      if (onlineVotingInfo.Status == OnlineBallotStatusEnum.Processed)
      {
        // already processed... don't do anything
        return new
        {
          Error = "Ballot already processed"
        }.AsJsonResult();
      }

      var utcNow = DateTime.UtcNow;
      if (currentElection.OnlineWhenOpen.AsUtc() <= utcNow && currentElection.OnlineWhenClose.AsUtc() > utcNow)
      {
        var onlineVoteHelper = new OnlineVoteHelper();
        var logHelper = new LogHelper();

        if (!EncryptionHelper.IsEncrypted(onlineVotingInfo.ListPool))
        {
          // upgrade previous record
          onlineVoteHelper.SetListPoolEncrypted(onlineVotingInfo);
        }

        if (locked)
        {
          // ensure we have enough votes
          var rawPool = onlineVoteHelper.GetDecryptedListPool(onlineVotingInfo, out var errorMessage);
          if (rawPool == null)
          {
            logHelper.Add("LockPool but pool is empty. " + errorMessage, true);
            return new
            {
              Error = "Pool is empty"
            }.AsJsonResult();
          }

          if (errorMessage.HasContent())
          {
            logHelper.Add(errorMessage, true);
            return new
            {
              Error = errorMessage
            }.AsJsonResult();
          }

          List<OnlineRawVote> completePool;
          try
          {
            completePool = JsonConvert.DeserializeObject<List<OnlineRawVote>>(rawPool);
          }
          catch (Exception e)
          {
            logHelper.Add("LockPool but pool has invalid JSON. " + e.GetBaseException().Message + "...  Start of pool: " + rawPool.Left(30), true);
            return new
            {
              Error = "Technical error in pool. Please edit and try again."
            }.AsJsonResult();
          }

          var numVotes = completePool.Count;
          if (numVotes < currentElection.NumberToElect)
          {
            var msg = $"Too few votes ({numVotes})";
            logHelper.Add(msg + $" Required ({currentElection.NumberToElect})", true);
            return new
            {
              Error = msg
            }.AsJsonResult();
          }
        }

        onlineVotingInfo.PoolLocked = locked;

        onlineVotingInfo.Status = locked ? OnlineBallotStatusEnum.Submitted : OnlineBallotStatusEnum.Draft;
        onlineVotingInfo.HistoryStatus += $";{onlineVotingInfo.Status} ({UserSession.VoterLoginSource})|{utcNow:u}";
        onlineVotingInfo.WhenStatus = utcNow;

        var personCacher = new PersonCacher(Db);
        var person = personCacher.AllForThisElection.SingleOrDefault(p => p.PersonGuid == onlineVotingInfo.PersonGuid);
        if (person == null)
        {
          return new
          {
            Error = "Invalid request (2)"
          }.AsJsonResult();
        }

        if (!person.Voter.CanVote.AsBoolean())
        {
          return new
          {
            Error = "Cannot vote"
          }.AsJsonResult();
        }

        var voter = person.Voter;

        var peopleModel = new PeopleModel();
        var votingMethodRemoved = false;
        string notificationType = null;
        var usingKiosk = UserSession.VoterIdType == VoterIdTypeEnum.Kiosk.Value;

        voter.HasOnlineBallot = locked;

        if (voter.VotingMethod.HasContent() && !(voter.VotingMethod == VotingMethodEnum.Online || voter.VotingMethod == VotingMethodEnum.Kiosk))
        {
          // teller has set. Voter can't change it...
        }
        else
        {
          if (locked)
          {
            voter.VotingMethod = usingKiosk ? VotingMethodEnum.Kiosk : VotingMethodEnum.Online;
            voter.RegistrationTime = utcNow;
            voter.VotingLocationGuid = new LocationModel().GetOnlineLocation().LocationGuid;
            voter.EnvNum = null;

            var log = voter.RegistrationLog;
            log.Add(new[]
            {
              voter.RegistrationTime.AsUtc().AsString("o"),
              UserSession.VoterLoginSource,
              VotingMethodEnum.TextFor(voter.VotingMethod),
            }.JoinedAsString("; ", true));
            voter.RegistrationLog = log;

            // logHelper.Add("Locked ballot");
            logHelper.Add("Submitted Ballot");

            if (UserSession.VoterIdType == VoterIdTypeEnum.Kiosk.Value)
            {
              voter.KioskCode = ""; // set to an empty string, not NULL - can tell the difference: used is ""
            }

            var notificationHelper = new NotificationHelper();
            var notificationSent = notificationHelper.SendWhenBallotSubmitted(person, currentElection, out notificationType, out var error);
            if (!notificationSent)
            {
              notificationType = null;
            }
          }
          else
          {
            // not online or anywhere
            voter.VotingMethod = null;
            voter.VotingLocationGuid = null;
            voter.EnvNum = null;

            votingMethodRemoved = true;

            var log = voter.RegistrationLog;
            voter.RegistrationTime = utcNow; // set time so that the log will have it
            log.Add(new[]
            {
              voter.RegistrationTime.AsUtc().AsString("o"),
              "Cancel Online",
            }.JoinedAsString("; ", true));
            voter.RegistrationTime = null; // don't keep it visible
            voter.RegistrationLog = log;

            // logHelper.Add("Unlocked ballot");
            logHelper.Add("Recalled Ballot");
          }
        }

        Db.SaveChanges();

        //TODO: verify that voter is saved

        personCacher.UpdateItemAndSaveCache(person);

        peopleModel.UpdateFrontDeskListing(person, votingMethodRemoved);


        // okay
        return new
        {
          success = true,
          notificationType,
          voter.VotingMethod,
          ElectionGuid = UserSession.CurrentElectionGuid,
          RegistrationTime = voter.RegistrationTime.AsUtc(),
          WhenStatus = onlineVotingInfo.WhenStatus.AsUtc(),
          onlineVotingInfo.PoolLocked
        }.AsJsonResult();
      }

      return new
      {
        Error = "Closed"
      }.AsJsonResult();
    }

    public JsonResult GetVoterElections()
    {
      var voterId = UserSession.VoterId;
      if (voterId.HasNoContent())
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var list = Db.Person
        .Join(Db.Voter, p => p.PersonGuid, v => v.PersonGuid, (p, v) =>
        new
        {
          p,
          v
        })
        // find this person
        .Where(pv => pv.p.Email == voterId || pv.p.Phone == voterId || pv.v.KioskCode == voterId)

        // TODO review this logic for 2 stage elections

        // and the elections they are in - for 2-stage, this will be the LSA2M election
        .Join(Db.Election, pv => pv.p.ElectionGuid, peopleElection => peopleElection.ElectionGuid,
          (p, peopleElection) => new { p.p, p.v, peopleElection })

        // and if there is a unit LSA2U election that they are in
        .GroupJoin(Db.Election, j => j.peopleElection.ElectionGuid, childElection => childElection.PeopleElectionGuid,
          (j, childList) => new
          {
            j.p,
            j.v,
            j.peopleElection,
            targetElection = childList.FirstOrDefault(e => e.ElectionType == ElectionTypeEnum.LSA2U && e.UnitName != null && e.UnitName == j.p.UnitName)
                             ?? j.peopleElection
          })

        // get the online voting info for this person 
        .GroupJoin(Db.OnlineVotingInfo, g => g.p.PersonGuid,
          ovi => ovi.PersonGuid,
          (g, oviList) => new
          {
            g.p,
            g.v,
            g.peopleElection,
            g.targetElection,
            ovi = oviList.FirstOrDefault()
          })

        .OrderByDescending(j => j.targetElection.OnlineWhenClose)
        .ThenByDescending(j => j.targetElection.DateOfElection)
        .ThenBy(j => j.p.C_RowId)
        .ToList()
        .Select(j => new
        {
          id = j.targetElection.ElectionGuid,
          peopleElectionId = j.peopleElection.ElectionGuid, // where the people records are
          j.targetElection.Name,
          j.targetElection.Convenor,
          j.targetElection.ElectionType,
          j.targetElection.UnitName, //TODO show this
          DateOfElection = j.targetElection.DateOfElection.AsUtc(),
          j.targetElection.EmailFromAddress,
          j.targetElection.EmailFromName,
          OnlineWhenOpen = j.targetElection.OnlineWhenOpen.AsUtc(),
          OnlineWhenClose = j.targetElection.OnlineWhenClose.AsUtc(),
          j.targetElection.OnlineCloseIsEstimate,
          //          j.e.TallyStatus,
          person = new
          {
            name = j.p.C_FullName,
            j.v.VotingMethod,
            RegistrationTime = j.v.RegistrationTime.AsUtc(),
            j.ovi?.PoolLocked,
            j.ovi?.Status,
            WhenStatus = j.ovi?.WhenStatus.AsUtc()
          }
        })
        .ToList();

      // piggyback and get other info too
      var emailCodes = Db.OnlineVoter.FirstOrDefault(ov => ov.VoterId == voterId)?.EmailCodes;
      // var hasLocalId = Db.AspNetUsers.Any(u => u.Email == voterId);

      return new
      {
        list,
        emailCodes,
        // hasLocalId
      }.AsJsonResult();
    }


    public JsonResult GetLoginHistory()
    {
      var uniqueId = UserSession.UniqueId;
      if (uniqueId.HasNoContent())
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var ageCutoff = DateTime.Today.Subtract(TimeSpan.FromDays(14)); // don't bother with UTC here
      var list = Db.C_Log
        .GroupJoin(Db.Election, log => log.ElectionGuid, e => e.ElectionGuid, (log, eList) => new
        {
          log,
          ElectionName = eList.FirstOrDefault().Name ?? (log.ElectionGuid == null ? "" : "(removed)")
        })
        .Where(j => j.log.VoterId == uniqueId)
        .Where(j => j.log.AsOf > ageCutoff)
        .Where(j => !j.log.Details.Contains("schema")) // hide error codes
        .OrderByDescending(j => j.log.AsOf)
        .Take(19)
        .ToList()
        .Select(j => new
        {
          AsOf = j.log.AsOf.AsUtc(),
          j.ElectionName,
          j.log.Details,
        });

      return new
      {
        list,
      }.AsJsonResult();
    }

    public JsonResult SaveEmailCodes(string emailCodes)
    {
      var onlineVoter = Db.OnlineVoter.Single(ov => ov.VoterId == UserSession.VoterId);
      onlineVoter.EmailCodes = emailCodes;
      Db.SaveChanges();

      return new
      {
        saved = true
      }.AsJsonResult();
    }

    public JsonResult SendTestMessage()
    {
      var notificationHelper = new NotificationHelper();
      var sent = notificationHelper.SendVoterTestMessage(out var error);

      return new
      {
        sent,
        Error = error
      }.AsJsonResult();
    }
  }
}