using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CsQuery.ExtensionMethods;
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

      var now = DateTime.Now;

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

      if (electionInfo.e.OnlineWhenOpen <= now && electionInfo.e.OnlineWhenClose > now)
      {
        // put election in session
        UserSession.CurrentElectionGuid = electionInfo.e.ElectionGuid;
        UserSession.VoterInElectionPersonGuid = electionInfo.p.PersonGuid;
        // UserSession.VoterInElectionPersonName = electionInfo.p.C_FullNameFL;

        if (votingInfo == null)
        {
          votingInfo = new OnlineVotingInfo
          {
            ElectionGuid = electionInfo.e.ElectionGuid,
            PersonGuid = electionInfo.p.PersonGuid,
            Status = OnlineBallotStatusEnum.New,
            WhenStatus = now,
            HistoryStatus = "New|{0}".FilledWith(now.ToJSON())
          };
          Db.OnlineVotingInfo.Add(votingInfo);
          Db.SaveChanges();
        }

        // okay
        return new
        {
          open = true,
          voterName = electionInfo.p.C_FullNameFL,
          electionInfo.e.NumberToElect,
          OnlineSelectionProcess = electionInfo.e.OnlineSelectionProcess.DefaultTo(OnlineSelectionProcessEnum.Random.ToString().Substring(0, 1)),
          registration = VotingMethodEnum.TextFor(electionInfo.p.VotingMethod),
          votingInfo
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

      var onlineVotingInfo = Db.OnlineVotingInfo
        .SingleOrDefault(ovi => ovi.ElectionGuid == electionGuid && ovi.PersonGuid == personGuid);

      if (onlineVotingInfo == null)
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var now = DateTime.Now;
      if (UserSession.CurrentElection.OnlineWhenOpen <= now && UserSession.CurrentElection.OnlineWhenClose > now)
      {
        onlineVotingInfo.ListPool = pool; // pool is JSON string

        var newStatus = pool == "[]" ? OnlineBallotStatusEnum.New : OnlineBallotStatusEnum.Draft;
        if (newStatus != onlineVotingInfo.Status)
        {
          onlineVotingInfo.Status = newStatus;
        }

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

      var now = DateTime.Now;
      if (currentElection.OnlineWhenOpen <= now && currentElection.OnlineWhenClose > now)
      {
        if (locked)
        {
          // ensure we have enough votes
          //TODO use JSON
          var votes = onlineVotingInfo.ListPool?.Split(',').Length ?? 0;
          if (votes < currentElection.NumberToElect)
          {
            return new
            {
              Error = "Too few votes"
            }.AsJsonResult();
          }
        }

        onlineVotingInfo.PoolLocked = locked;

        onlineVotingInfo.Status = locked ? OnlineBallotStatusEnum.Submitted : OnlineBallotStatusEnum.Draft;
        onlineVotingInfo.HistoryStatus += $";{onlineVotingInfo.Status} ({UserSession.VoterLoginSource})|{now.ToJSON()}".FilledWith(onlineVotingInfo.Status, now.ToJSON());
        if (locked)
        {
          onlineVotingInfo.WhenStatus = now;
        }
        else
        {
          onlineVotingInfo.WhenStatus = null;
        }

        var personCacher = new PersonCacher(Db);
        var person = personCacher.AllForThisElection.SingleOrDefault(p => p.PersonGuid == onlineVotingInfo.PersonGuid);
        if (person == null || !person.CanVote.AsBoolean())
        {
          return new
          {
            Error = "Invalid request (2)"
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
            person.RegistrationTime = now;
            person.VotingLocationGuid = new LocationModel().GetOnlineLocation().LocationGuid;
            person.EnvNum = null;

            var log = person.RegistrationLog;
            log.Add(new[]
            {
              peopleModel.ShowRegistrationTime(person, true),
              UserSession.VoterLoginSource,
              VotingMethodEnum.TextFor(person.VotingMethod),
            }.JoinedAsString("; ", true));
            person.RegistrationLog = log;

            new LogHelper().Add("Locked ballot");

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
            person.RegistrationTime = now; // set time so that the log will have it
            log.Add(new[]
            {
              peopleModel.ShowRegistrationTime(person, true),
              "Cancel Online",
            }.JoinedAsString("; ", true));
            person.RegistrationTime = null; // don't keep it visible
            person.RegistrationLog = log;

            new LogHelper().Add("Unlocked ballot");
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
          j.e.OnlineWhenOpen,
          j.e.OnlineWhenClose,
          j.e.OnlineCloseIsEstimate,
          //          j.e.TallyStatus,
          person = new
          {
            name = j.p.C_FullNameFL,
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
      var voterId = UserSession.VoterId;
      if (voterId.HasNoContent())
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var ageCutoff = DateTime.Today.Subtract(TimeSpan.FromDays(14));
      var list = Db.C_Log
        .Where(log => log.VoterId == voterId)
        .Where(log => log.AsOf > ageCutoff)
        .Where(log => !log.Details.Contains("schema")) // hide error codes
        .OrderByDescending(log => log.AsOf)
        .Take(19)
        .Select(log => new
        {
          log.AsOf,
          log.Details,
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