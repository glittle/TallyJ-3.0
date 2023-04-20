using System;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  [ForSysAdmin]
  public class SysAdminController : BaseController
  {
    public ActionResult Index()
    {
      return View();
    }

    public JsonResult GetMainLog(string searchText, string searchName, int lastRowId = 0, int numToShow = 50, DateTime? fromDate = null, DateTime? toDate = null)
    {
      var dbContext = Db;

      var query1 = dbContext.C_Log
        .GroupJoin(dbContext.Election, l => l.ElectionGuid, e => e.ElectionGuid, (l, eList) => new { l, e = eList.FirstOrDefault() });

      if (lastRowId != 0)
      {
        query1 = query1.Where(j => j.l.C_RowId < lastRowId);
      }

      if (fromDate.HasValue && toDate.HasValue)
      {
        var from = fromDate.Value.AsUtc();
        var to = toDate.Value.AsUtc();
        query1 = query1.Where(j => j.l.AsOf >= from && j.l.AsOf <= to);
      }

      if (searchText.HasContent())
      {
        searchText = $"%{searchText}%";
        query1 = query1.Where(j => SqlFunctions.PatIndex(searchText, j.l.Details) > 0
        );
      }

      if (searchName.HasContent())
      {
        searchName = $"%{searchName}%";
        query1 = query1.Where(j => SqlFunctions.PatIndex(searchName, j.l.VoterId) > 0
                                   || SqlFunctions.PatIndex(searchName, j.e.Convenor) > 0
                                   || SqlFunctions.PatIndex(searchName, j.e.Name) > 0
        );
      }

      var list = query1
        .OrderByDescending(j => j.l.C_RowId)
        .Take(numToShow)
        .ToList();

      var logLines = list
        .Select(j => new
        {
          j.l.C_RowId,
          AsOf = j.l.AsOf.AsUtc(),
          j.l.Details,
          ElectionName = j.e?.Name,
          j.l.HostAndVersion,
          //          isVoter = j.l.VoterEmail == null ? null : "Voter",
          j.l.VoterId,
          j.l.ComputerCode
        }).ToList();

      return new
      {
        logLines,
        Success = true
      }.AsJsonResult();
    }

    public JsonResult GetOnlineVotingLog(int numToShow = 50)
    {
      var dbContext = Db;

      var logLines = dbContext.Election
        .GroupJoin(dbContext.JoinElectionUser
            .Join(dbContext.Memberships, j => j.UserId, m => m.UserId, (j, m) => new { j.ElectionGuid, m.Email, j.Role })
          , e => e.ElectionGuid, jeu => jeu.ElectionGuid, (e, jeuList) => new { e, adminEmails = jeuList.Select(x => new { x.Email, x.Role }) })
        .GroupJoin(dbContext.OnlineVotingInfo, j => j.e.ElectionGuid, ovi => ovi.ElectionGuid, (j, oviList) => new
        {
          j.e,
          j.adminEmails,
          oviList
        })
        //.Where(j => j.oviList.Any())
        .Where(j => j.e.OnlineWhenOpen != null)
        .OrderByDescending(j => j.e.OnlineWhenClose)
        .Take(numToShow)
        .Select(j => new
        {
          j.e.Name,
          j.e.Convenor,
          j.adminEmails,
          j.e.TallyStatus,
          j.e.OnlineWhenOpen,
          j.e.OnlineWhenClose,
          j.e.NumberToElect,
          Activated = j.oviList.Count(),
          Submitted = j.oviList.Count(ovi => ovi.Status == OnlineBallotStatusEnum.Submitted.Value),
          Processed = j.oviList.Count(ovi => ovi.Status == OnlineBallotStatusEnum.Processed.Value),
          First = j.oviList.Min(ovi => ovi.WhenStatus),
          MostRecent = j.oviList.Max(ovi => ovi.WhenStatus)
        })
        .ToList()
        .Select(x => new
        {
          x.Name,
          x.Convenor,
          Email = x.adminEmails.Select(ae => $"{ae.Email} [{ae.Role.DefaultTo("Owner")}]").JoinedAsString(", "),
          x.TallyStatus,
          OnlineWhenOpen = x.OnlineWhenOpen.AsUtc(),
          OnlineWhenClose = x.OnlineWhenClose.AsUtc(),
          x.NumberToElect,
          x.Activated,
          x.Submitted,
          x.Processed,
          First = x.First.AsUtc(),
          MostRecent = x.MostRecent.AsUtc()
        });

      return new
      {
        logLines,
        Success = true
      }.AsJsonResult();
    }

    public JsonResult GetElectionList(int numToShow = 50)
    {
      var dbContext = Db;

      var logLines = dbContext.Election
        .GroupJoin(dbContext.JoinElectionUser
            .Join(dbContext.Memberships, j => j.UserId, m => m.UserId, (j, m) => new { j.ElectionGuid, m.Email, j.Role })
          , e => e.ElectionGuid, jeu => jeu.ElectionGuid, (e, jeuList) => new { e, adminEmails = jeuList.Select(x => new { x.Email, x.Role }) })
        .GroupJoin(dbContext.OnlineVotingInfo, j => j.e.ElectionGuid, ovi => ovi.ElectionGuid, (j, oviList) => new
        {
          j.e,
          j.adminEmails,
          NumOnline = oviList.Count()
        })
        .GroupJoin(dbContext.Person, j => j.e.ElectionGuid, j => j.ElectionGuid, (j, pList) => new
        {
          j.e,
          j.adminEmails,
          j.NumOnline,
          NumPeople = pList.Count()
        })
        .GroupJoin(dbContext.Location
          .Join(dbContext.Ballot, l => l.LocationGuid, b => b.LocationGuid, (l, b) => new
          {
            l.ElectionGuid,
            b.BallotGuid
          })
          .GroupJoin(dbContext.Vote, j => j.BallotGuid, v => v.BallotGuid, (j, vList) =>
              new
              {
                j.ElectionGuid,
                SingleNameCount = vList.Sum(v => v.SingleNameElectionCount),
              }),
          j => j.e.ElectionGuid, lb => lb.ElectionGuid, (j, lbList) => new
          {
            j.e,
            j.NumPeople,
            j.NumOnline,
            j.adminEmails,
            NumBallots = j.e.NumberToElect == 1 && j.e.NumberExtra == 0
              ? lbList.Sum(l => l.SingleNameCount)
              : lbList.Count()
          })
        .GroupJoin(dbContext.C_Log, j => j.e.ElectionGuid, l => l.ElectionGuid, (j, lList) => new
        {
          j.e,
          j.adminEmails,
          j.NumPeople,
          j.NumOnline,
          j.NumBallots,
          RecentActivity = lList.Max(l => (DateTime?)l.AsOf)
        })
        .OrderByDescending(j => j.e.DateOfElection)
        .ThenBy(j => j.e.Name)
        .Take(numToShow)
        .ToList()
        .Select(j => new
        {
          j.e.C_RowId,
          j.e.Name,
          j.e.Convenor,
          DateOfElection = j.e.DateOfElection.AsUtc(),
          Email = j.adminEmails.Select(ae => $"{ae.Email} [{ae.Role.DefaultTo("Owner")}]").JoinedAsString(", "),
          j.e.ElectionType,
          j.e.ElectionMode,
          j.e.ShowAsTest,
          j.e.TallyStatus,
          j.e.NumberToElect,
          j.NumOnline,
          j.NumBallots,
          j.NumPeople,
          RecentActivity = j.RecentActivity.AsUtc()
        }).ToList();

      return new
      {
        logLines,
        Success = true
      }.AsJsonResult();




      //      NumBallots = dbContext.Location
      //        .Where(l => l.ElectionGuid == j.e.ElectionGuid)
      //        .GroupJoin(dbContext.Ballot, l => l.LocationGuid, b => b.LocationGuid, (l, bList) => new
      //        {
      //          bCount = bList.Count()
      //        })
      //        .Sum(g => g.bCount),
      //      NumPeople = dbContext.Person
      //        .Count(l => l.ElectionGuid == j.e.ElectionGuid),
      //      RecentActivity = dbContext.C_Log
      //        .Where(l => l.ElectionGuid == j.e.ElectionGuid)
      //        .Max(l => l.AsOf)
    }

    public ActionResult GetUnconnectedVoters()
    {
      var dbContext = Db;

      // var onlineBallots = dbContext.OnlineVotingInfo
      //   .Select(ovi => new { ovi.PersonGuid });


      var connectedVotersEmail = dbContext.OnlineVoter
        .Join(dbContext.Person, ov => ov.VoterId, p => p.Email, (ov, p) => new { ov.C_RowId })
        .Select(x => x.C_RowId)
        .ToList();
      var connectedVotersPhone = dbContext.OnlineVoter
        .Join(dbContext.Person, ov => ov.VoterId, p => p.Phone, (ov, p) => new { ov.C_RowId })
        .Select(x => x.C_RowId)
        .ToList();

      var connectedIds = connectedVotersEmail.Union(connectedVotersPhone).ToList();
      var wantedTypes = new[] { VoterIdTypeEnum.Email.Value, VoterIdTypeEnum.Phone.Value };

      var logLines = dbContext.OnlineVoter
        .Where(ov => wantedTypes.Contains(ov.VoterIdType) && !connectedIds.Contains(ov.C_RowId))
        .Select(ov => new
        {
          ov.C_RowId,
          Email = ov.VoterIdType == VoterIdTypeEnum.Email.Value ? ov.VoterId : null,
          Phone = ov.VoterIdType == VoterIdTypeEnum.Phone.Value ? ov.VoterId : null,
          ov.Country,
          ov.WhenRegistered,
          ov.WhenLastLogin,
        })
        .ToList();

      return new
      {
        logLines,
        Success = true
      }.AsJsonResult();

    }
  }
}