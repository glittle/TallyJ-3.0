using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  [ForSysAdmin]
  public class AdminController : BaseController
  {
    public ActionResult Index()
    {
      return View();
    }

    public JsonResult GetLog(int lastRowId = 0)
    {
      var dbContext = Db;

      return new
      {
        list = dbContext.C_Log
          .Where(l => l.C_RowId < lastRowId || lastRowId == 0)
          .GroupJoin(dbContext.Election, l => l.ElectionGuid, e => e.ElectionGuid, (l, eList) => new { l, e = eList.FirstOrDefault() ?? new Election() })
          .OrderByDescending(j => j.l.C_RowId)
          .Take(50)
          .Select(j => new
          {
            j.l.C_RowId,
            j.l.AsOf,
            j.l.Details,
            j.e.Name,
            j.l.HostAndVersion,
            isVoter = j.l.VoterEmail == null ? null : "Voter",
          })
      }.AsJsonResult();
    }
  }
}