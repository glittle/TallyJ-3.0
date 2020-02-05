using System;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
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
  }
}