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
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  [AllowVoter]
  public class VoterController : BaseController
  {
    public ActionResult Index()
    {
      return View("VoterHome");
    }

    public void JoinVoterHubs(string connId)
    {
      new AllVotersHub().Join(connId);
      new VoterPersonalHub().Join(connId);
    }

    public JsonResult JoinElection(Guid electionGuid)
    {
      var email = UserSession.VoterEmail;
      if (email.HasNoContent())
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      // confirm that this person is in the election
      var electionInfo = Db.Person.Where(p => p.Email == email && p.ElectionGuid == electionGuid)
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

      var otherVotingInfo = Db.OnlineVotingInfo
        .SingleOrDefault(ovi => ovi.ElectionGuid == electionGuid && ovi.Email == electionInfo.p.Email);

      if (votingInfo == null && otherVotingInfo != null)
      {
        return new
        {
          Error = "This email address was used for another person."
        }.AsJsonResult();
      }

      if (electionInfo.e.OnlineWhenOpen <= now && electionInfo.e.OnlineWhenClose > now)
      {
        // put election in session
        UserSession.CurrentElectionGuid = electionInfo.e.ElectionGuid;

        if (votingInfo == null)
        {
          votingInfo = new OnlineVotingInfo
          {
            ElectionGuid = electionInfo.e.ElectionGuid,
            PersonGuid = electionInfo.p.PersonGuid,
            Email = email,
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
          electionInfo.e.NumberToElect,
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

      // leave hub


      return new
      {
        success = true
      }.AsJsonResult();
    }

    public JsonResult SavePool(string pool)
    {
      var electionGuid = UserSession.CurrentElectionGuid;
      var email = UserSession.VoterEmail;

      var votingInfo = Db.OnlineVotingInfo.SingleOrDefault(ovi => ovi.ElectionGuid == electionGuid && ovi.Email == email);

      if (votingInfo == null)
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var now = DateTime.Now;
      if (UserSession.CurrentElection.OnlineWhenOpen <= now && UserSession.CurrentElection.OnlineWhenClose > now)
      {
        votingInfo.ListPool = pool;
        Db.SaveChanges();

        // okay
        return new
        {
          success = true,
        }.AsJsonResult();
      }

      return new
      {
        Error = "Closed"
      }.AsJsonResult();
    }

    public JsonResult LockPool(bool locked)
    {
      var currentElection = UserSession.CurrentElection;
      var email = UserSession.VoterEmail;

      var onlineVotingInfo = Db.OnlineVotingInfo.SingleOrDefault(ovi => ovi.ElectionGuid == currentElection.ElectionGuid && ovi.Email == email);

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

        onlineVotingInfo.Status = locked ? OnlineBallotStatusEnum.Ready : OnlineBallotStatusEnum.Pending;
        onlineVotingInfo.HistoryStatus += ";{0}|{1}".FilledWith(onlineVotingInfo.Status, now.ToJSON());
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
        if (person == null || !person.CanVote.GetValueOrDefault())
        {
          return new
          {
            Error = "Invalid request (2)"
          }.AsJsonResult();
        }
        Db.Person.Attach(person);
        var peopleModel = new PeopleModel();
        var votingMethodRemoved = false;

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
              peopleModel.ShowRegistrationTime(person),
              VotingMethodEnum.TextFor(person.VotingMethod),
            }.JoinedAsString("; ", true));
            person.RegistrationLog = log;

            new LogHelper().Add("Locked ballot");
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
              peopleModel.ShowRegistrationTime(person),
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
      var email = UserSession.VoterEmail;
      if (email.HasNoContent())
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var list = Db.Person
        .Where(p => p.Email == email && p.CanVote == true && p.IneligibleReasonGuid == null)
        .Join(Db.Election, p => p.ElectionGuid, e => e.ElectionGuid, (p, e) => new { p, e })
        .GroupJoin(Db.OnlineVotingInfo, g => g.p.PersonGuid, ovi => ovi.PersonGuid, (g, oviList) => new { g.p, g.e, ovi = oviList.FirstOrDefault() })
        .OrderByDescending(j => j.e.OnlineWhenOpen)
        .ThenByDescending(j => j.e.DateOfElection)
        .ThenBy(j => j.p.C_RowId)
        .Select(j => new
        {
          id = j.e.ElectionGuid,
          j.e.Name,
          j.e.Convenor,
          j.e.ElectionType,
          j.e.TallyStatus,
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

      return new
      {
        list
      }.AsJsonResult();
    }


    public JsonResult GetLoginHistory()
    {
      var email = UserSession.VoterEmail;
      if (email.HasNoContent())
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var ageCutoff = DateTime.Today.Subtract(TimeSpan.FromDays(14));
      var list = Db.C_Log
        .Where(log => log.VoterEmail == email)
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
        list
      }.AsJsonResult();
    }


  }
}