using System;
using System.Collections.Generic;
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

    public JsonResult JoinElection(Guid electionGuid)
    {
      var voterId = UserSession.VoterId;
      if (voterId.HasNoContent())
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      // confirm that this person is in the election
      var personQuery = Db.Person.Where(p => p.ElectionGuid == electionGuid);

      if (UserSession.VoterIdType == VoterIdTypeEnum.Email)
      {
        personQuery = personQuery.Where(p => p.Email == voterId);
      }
      else if (UserSession.VoterIdType == VoterIdTypeEnum.Phone)
      {
        personQuery = personQuery.Where(p => p.Phone == voterId);
      }
      else
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var electionInfo = personQuery
        .Join(Db.Election, p => p.ElectionGuid, e => e.ElectionGuid, (p, e) => new { e, p })
        .SingleOrDefault();

      if (electionInfo == null)
      {
        return new
        {
          Error = "Invalid election"
        }.AsJsonResult();
      }

      var utcNow = DateTime.UtcNow;

      // get voting info
      var votingInfo = Db.OnlineVotingInfo
        .SingleOrDefault(ovi => ovi.ElectionGuid == electionGuid && ovi.PersonGuid == electionInfo.p.PersonGuid);

      // if (votingInfo == null)
      // {
      //   var existingByEmail = Db.OnlineVotingInfo
      //     .SingleOrDefault(ovi => ovi.ElectionGuid == electionGuid && ovi.PersonGuid == electionInfo.p.Email);
      //
      //   if (existingByEmail != null)
      //   {
      //     return new
      //     {
      //       Error = "This email address was used for another person."
      //     }.AsJsonResult();
      //   }
      //
      //   var existingByPhone = Db.OnlineVotingInfo
      //      .SingleOrDefault(ovi => ovi.ElectionGuid == electionGuid && ovi.Phone == electionInfo.p.Phone);
      //
      //   if (existingByPhone != null)
      //   {
      //     return new
      //     {
      //       Error = "This phone number was used for another person."
      //     }.AsJsonResult();
      //   }
      // }

      if (electionInfo.e.OnlineWhenOpen.FromSql() <= utcNow && electionInfo.e.OnlineWhenClose.FromSql() > utcNow)
      {
        // put election in session
        UserSession.CurrentElectionGuid = electionInfo.e.ElectionGuid;
        UserSession.VoterInElectionPersonGuid = electionInfo.p.PersonGuid;
        // UserSession.VoterInElectionPersonName = electionInfo.p.C_FullNameFL;
        string poolDecryptError = null;

        if (votingInfo == null)
        {
          votingInfo = new OnlineVotingInfo
          {
            ElectionGuid = electionInfo.e.ElectionGuid,
            PersonGuid = electionInfo.p.PersonGuid,
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
          voterName = electionInfo.p.C_FullName,
          electionInfo.e.NumberToElect,
          OnlineSelectionProcess = electionInfo.e.OnlineSelectionProcess.DefaultTo(OnlineSelectionProcessEnum.Random.ToString().Substring(0, 1)),
          registration = VotingMethodEnum.TextFor(electionInfo.p.VotingMethod),
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

      var now = DateTime.Now;
      var utcNow = DateTime.UtcNow;
      if (UserSession.CurrentElection.OnlineWhenOpen <= now && UserSession.CurrentElection.OnlineWhenClose > now)
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
      if (currentElection.OnlineWhenOpen.FromSql() <= utcNow && currentElection.OnlineWhenClose.FromSql() > utcNow)
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

        if (!person.CanVote.AsBoolean())
        {
          return new
          {
            Error = "Cannot vote"
          }.AsJsonResult();
        }

        Db.Person.Attach(person);
        var peopleModel = new PeopleModel();
        var votingMethodRemoved = false;
        string notificationType = null;

        person.HasOnlineBallot = locked;

        if (person.VotingMethod.HasContent() && person.VotingMethod != VotingMethodEnum.Online)
        {
          // teller has set. Voter can't change it...
        }
        else
        {
          if (locked)
          {
            person.VotingMethod = VotingMethodEnum.Online;
            person.RegistrationTime = utcNow;
            person.VotingLocationGuid = new LocationModel().GetOnlineLocation().LocationGuid;
            person.EnvNum = null;

            var log = person.RegistrationLog;
            log.Add(new[]
            {
              peopleModel.ShowRegistrationTime(person),
              UserSession.VoterLoginSource,
              VotingMethodEnum.TextFor(person.VotingMethod),
            }.JoinedAsString("; ", true));
            person.RegistrationLog = log;

            // logHelper.Add("Locked ballot");
            logHelper.Add("Submitted Ballot");


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
            person.VotingMethod = null;
            person.VotingLocationGuid = null;
            person.EnvNum = null;

            votingMethodRemoved = true;

            var log = person.RegistrationLog;
            person.RegistrationTime = utcNow; // set time so that the log will have it
            log.Add(new[]
            {
              peopleModel.ShowRegistrationTime(person),
              "Cancel Online",
            }.JoinedAsString("; ", true));
            person.RegistrationTime = null; // don't keep it visible
            person.RegistrationLog = log;

            // logHelper.Add("Unlocked ballot");
            logHelper.Add("Recalled Ballot");
          }
        }

        Db.SaveChanges();

        personCacher.UpdateItemAndSaveCache(person);
        peopleModel.UpdateFrontDeskListing(person, votingMethodRemoved);


        // okay
        return new
        {
          success = true,
          notificationType,
          person.VotingMethod,
          ElectionGuid = UserSession.CurrentElectionGuid,
          person.RegistrationTime,
          onlineVotingInfo.WhenStatus,
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
        .Where(p => (p.Email == voterId || p.Phone == voterId) && p.CanVote == true)
        .Join(Db.Election, p => p.ElectionGuid, e => e.ElectionGuid, (p, e) => new { p, e })
        .GroupJoin(Db.OnlineVotingInfo, g => g.p.PersonGuid, ovi => ovi.PersonGuid, (g, oviList) => new { g.p, g.e, ovi = oviList.FirstOrDefault() })
        .OrderByDescending(j => j.e.OnlineWhenClose)
        .ThenByDescending(j => j.e.DateOfElection)
        .ThenBy(j => j.p.C_RowId)
        .Select(j => new
        {
          id = j.e.ElectionGuid,
          j.e.Name,
          j.e.Convenor,
          j.e.ElectionType,
          j.e.DateOfElection,
          j.e.EmailFromAddress,
          j.e.EmailFromName,
          j.e.OnlineWhenOpen, //TODO UTC??
          j.e.OnlineWhenClose,
          j.e.OnlineCloseIsEstimate,
          //          j.e.TallyStatus,
          person = new
          {
            name = j.p.C_FullName,
            j.p.VotingMethod,
            j.p.RegistrationTime,
            j.ovi.PoolLocked,
            j.ovi.Status,
            j.ovi.WhenStatus
          }
        });

      // piggyback and get other info too
      var emailCodes = Db.OnlineVoter.Single(ov => ov.VoterId == voterId).EmailCodes;
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

      var ageCutoff = DateTime.Today.Subtract(TimeSpan.FromDays(14));
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
          AsOf = j.log.AsOf.FromSql(),
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