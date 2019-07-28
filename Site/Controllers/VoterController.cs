using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CsQuery.ExtensionMethods;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
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
        .Join(Db.OnlineElection, j => j.e.ElectionGuid, oe => oe.ElectionGuid, (j, oe) => new { j.e, j.p, oe })
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

      if (electionInfo.oe.WhenOpen <= now && electionInfo.oe.WhenClose > now)
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
            Status = "New",
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
          UserSession.CurrentElection.NumberToElect,
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

      var votingInfo = Db.OnlineVotingInfo
        .Where(ovi => ovi.ElectionGuid == electionGuid && ovi.Email == email)
        .Join(Db.OnlineElection, ovi => ovi.ElectionGuid, oe => oe.ElectionGuid, (ovi, oe) => new { ovi, oe })
        .SingleOrDefault();

      if (votingInfo == null)
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var now = DateTime.Now;
      if (votingInfo.oe.WhenOpen <= now && votingInfo.oe.WhenClose > now)
      {
        votingInfo.ovi.ListPool = pool;
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
      var electionGuid = UserSession.CurrentElectionGuid;
      var email = UserSession.VoterEmail;

      var votingInfo = Db.OnlineVotingInfo
        .Where(ovi => ovi.ElectionGuid == electionGuid && ovi.Email == email)
        .Join(Db.OnlineElection, ovi => ovi.ElectionGuid, oe => oe.ElectionGuid, (ovi, oe) => new { ovi, oe })
        .SingleOrDefault();

      if (votingInfo == null)
      {
        return new
        {
          Error = "Invalid request"
        }.AsJsonResult();
      }

      var now = DateTime.Now;
      if (votingInfo.oe.WhenOpen <= now && votingInfo.oe.WhenClose > now)
      {
        if (locked)
        {
          // ensure we have enough votes
          var votes = votingInfo.ovi.ListPool?.Split(',').Length ?? 0;
          if (votes < UserSession.CurrentElection.NumberToElect)
          {
            return new
            {
              Error = "Too few votes"
            }.AsJsonResult();
          }
        }

        votingInfo.ovi.PoolLocked = locked;

        votingInfo.ovi.Status = locked ? "Ready" : "Pending";
        votingInfo.ovi.HistoryStatus += ";{0}|{1}".FilledWith(votingInfo.ovi.Status, now.ToJSON());

        var personCacher = new PersonCacher(Db);
        var person = personCacher.AllForThisElection.SingleOrDefault(p => p.PersonGuid == votingInfo.ovi.PersonGuid);
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

        if (locked)
        {
          person.VotingMethod = VotingMethodEnum.Online;
          person.RegistrationTime = DateTime.Now;
          person.VotingLocationGuid = null; // online isn't treated as a location
          person.EnvNum = null;

          var log = person.RegistrationLog;
          log.Add(new[]
          {
            peopleModel.ShowRegistrationTime(person),
            VotingMethodEnum.TextFor(person.VotingMethod),
          }.JoinedAsString("; ", true));
          person.RegistrationLog = log;
        }
        else
        {
          // not online or anywhere
          person.VotingMethod = null;
          person.RegistrationTime = DateTime.Now;
          person.VotingLocationGuid = null; // online isn't treated as a location
          person.EnvNum = null;
          votingMethodRemoved = true;

          var log = person.RegistrationLog;
          log.Add(new[]
          {
            peopleModel.ShowRegistrationTime(person),
            "Cancel Online",
          }.JoinedAsString("; ", true));
          person.RegistrationLog = log;
        }

        personCacher.UpdateItemAndSaveCache(person);

        peopleModel.UpdateFrontDeskListing(person, votingMethodRemoved);

        Db.SaveChanges();

        // okay
        return new
        {
          success = true,
          registration = VotingMethodEnum.TextFor(person.VotingMethod)
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
        .Where(p => p.Email == email)
        .GroupBy(p => p.ElectionGuid, pList => pList)
        .Select(g => new
        {
          ElectionGuid = g.Key,
          // only one entry per election
          g.OrderBy(p => p.C_RowId).FirstOrDefault().PersonGuid
        })
        .Concat(Db.OnlineVotingInfo.Select(ovi => new
        {
          ovi.ElectionGuid,
          ovi.PersonGuid
        }))
        .Distinct()
        .GroupJoin(Db.OnlineElection, ep => ep.ElectionGuid, oe => oe.ElectionGuid,
          (ep, oeList) => new { ep.ElectionGuid, ep.PersonGuid, onlineElection = oeList.FirstOrDefault() })
        .GroupJoin(Db.Election, g => g.ElectionGuid, e => e.ElectionGuid, (g, eList) => new { g.ElectionGuid, g.PersonGuid, g.onlineElection, fullElection = eList.FirstOrDefault() })
        .GroupJoin(Db.Person, j => j.PersonGuid, p => p.PersonGuid, (j, pList) => new { j.ElectionGuid, j.PersonGuid, j.onlineElection, j.fullElection, p = pList.FirstOrDefault() })
        .OrderByDescending(j => j.onlineElection.HistoryWhen)
        .ThenByDescending(j => j.onlineElection.WhenOpen)
        .ThenByDescending(j => j.fullElection.DateOfElection)
        .ThenBy(j => j.p.C_RowId)
        .Select(j => new
        {
          id = j.fullElection != null ? j.fullElection.ElectionGuid : j.onlineElection.ElectionGuid,
          name = j.fullElection != null ? j.fullElection.Name : j.onlineElection.ElectionName,
          online = j.onlineElection != null ? new
          {
            j.onlineElection.WhenOpen,
            j.onlineElection.WhenClose,
            j.onlineElection.CloseIsEstimate,
            j.onlineElection.AllowResultView
          } : null,
          person = new
          {
            name = j.p.C_FullNameFL,
            j.p.VotingMethod,
            j.p.RegistrationTime
          }
        });

      return new
      {
        list
      }.AsJsonResult();
    }
  }
}