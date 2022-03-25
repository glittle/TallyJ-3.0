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
        query1 = query1.Where(j => j.l.AsOf.FromSql() >= fromDate.Value && j.l.AsOf.FromSql() <= toDate.Value);
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

      var logLines = query1
        .OrderByDescending(j => j.l.C_RowId)
        .Take(numToShow)
        .Select(j => new
        {
          j.l.C_RowId,
          AsOf = j.l.AsOf.FromSql(),
          j.l.Details,
          ElectionName = j.e.Name,
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
        .Join(dbContext.JoinElectionUser, e => e.ElectionGuid, jeu => jeu.ElectionGuid, (e, jeu) => new { e, jeu.UserId })
        .Join(dbContext.Memberships, j => j.UserId, m => m.UserId, (j, m) => new { j.e, m.Email })
        .GroupJoin(dbContext.OnlineVotingInfo, j => j.e.ElectionGuid, ovi => ovi.ElectionGuid, (j, oviList) => new
        {
          j.e,
          j.Email,
          oviList
        })
        .Where(j => j.oviList.Any())
        .OrderByDescending(j => j.e.OnlineWhenClose)
        .Take(numToShow)
        .Select(j => new
        {
          j.e.Name,
          j.e.Convenor,
          j.Email,
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
          x.Email,
          x.TallyStatus,
          OnlineWhenOpen = x.OnlineWhenOpen.FromSql(),
          OnlineWhenClose = x.OnlineWhenClose.FromSql(),
          x.NumberToElect,
          x.Activated,
          x.Submitted,
          x.Processed,
          x.First,
          x.MostRecent
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
        .Join(dbContext.JoinElectionUser, e => e.ElectionGuid, jeu => jeu.ElectionGuid, (e, jeu) => new { e, jeu.UserId })
        .Join(dbContext.Memberships, j => j.UserId, m => m.UserId, (j, m) => new { j.e, m.Email })
        .GroupJoin(dbContext.OnlineVotingInfo, j => j.e.ElectionGuid, ovi => ovi.ElectionGuid, (j, oviList) => new
        {
          j.e,
          j.Email,
          NumOnline = oviList.Count()
        })
        .GroupJoin(dbContext.Person, j => j.e.ElectionGuid, j => j.ElectionGuid, (j, pList) => new
        {
          j.e,
          j.Email,
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
            j.Email,
            NumBallots = j.e.NumberToElect == 1 && j.e.NumberExtra == 0
              ? lbList.Sum(l => l.SingleNameCount)
              : lbList.Count()
          })
        .GroupJoin(dbContext.C_Log, j => j.e.ElectionGuid, l => l.ElectionGuid, (j, lList) => new
        {
          j.e,
          j.Email,
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
          j.e.DateOfElection,
          j.Email,
          j.e.ElectionType,
          j.e.ElectionMode,
          j.e.ShowAsTest,
          j.e.TallyStatus,
          j.e.NumberToElect,
          j.NumOnline,
          j.NumBallots,
          j.NumPeople,
          RecentActivity = j.RecentActivity.FromSql()
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
  }
}