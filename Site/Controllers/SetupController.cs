using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.EF;
using TallyJ.Models;

namespace TallyJ.Controllers
{
  public class SetupController : BaseController
  {
    //
    // GET: /Setup/

    readonly string[] _fieldsToEdit = new[]
                                        {
                                          "Name",
                                          "DateOfElection",
                                          "Convenor",
                                          "ElectionType",
                                          "ElectionMode",
                                          "NumberToElect",
                                          "NumberExtra",
                                          "CanVote",
                                          "CanReceive",
                                        };

    public ActionResult Index()
    {
      return View("Setup");
    }

    public JsonResult SaveElection(Election election)
    {
      var onFile = DbContext.Elections.Where(e => e.C_RowId == election.C_RowId).SingleOrDefault();
      if (onFile != null)
      {
        // apply changes
        if (election.CopyPropertyValuesTo(onFile, _fieldsToEdit))
        {
          DbContext.SaveChanges();
        }

        return new
                 {
                   Status = "Saved",
                   Election = onFile
                 }.AsJsonResult();
      }

      return new
               {
                 Status = "Unkown ID"
               }.AsJsonResult();
    }

    public JsonResult DetermineRules(string type, string mode)
    {
      return new ElectionModel().GetRules(type, mode).AsJsonResult();
    }
  }
}