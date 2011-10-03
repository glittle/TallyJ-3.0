using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  public class SetupController : BaseController
  {
    //
    // GET: /Setup/

    public ActionResult Index()
    {
      //TODO: the client should already have this info...
      ContextItems.AddJavascriptForPage("setupIndexInfo", "setupIndexPage.info={0};".FilledWith(
        new
          {
            Election = UserSession.CurrentElection
          }.SerializedAsJson()));
      return View("Setup");
    }

    public JsonResult SaveElection(Election election)
    {
      var onFile = DbContext.Elections.Where(e => e.C_RowId == election.C_RowId).SingleOrDefault();
      if (onFile != null)
      {
        return new
        {
          Status = "Test",
          Election = onFile
        }.AsJsonResult();
      }

      return new
               {
                 Status = "Unkown ID"
               }.AsJsonResult();
    }
  }
}