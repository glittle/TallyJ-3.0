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

    public JsonResult GetMainLog(int lastRowId = 0)
    {
      var dbContext = Db;

      var logLines = dbContext.C_Log
        .Where(l => l.C_RowId < lastRowId || lastRowId == 0)
        .GroupJoin(dbContext.Election, l => l.ElectionGuid, e => e.ElectionGuid, (l, eList) => new { l, e = eList.FirstOrDefault() })
        .OrderByDescending(j => j.l.C_RowId)
        .Take(25)
        .Select(j => new
        {
          j.l.C_RowId,
          j.l.AsOf,
          j.l.Details,
          ElectionName = j.e.Name,
          j.l.HostAndVersion,
          //          isVoter = j.l.VoterEmail == null ? null : "Voter",
          j.l.VoterEmail,
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
        }).ToList();

      return new
      {
        logLines,
        Success = true
      }.AsJsonResult();
    }
  }
}