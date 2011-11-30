using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.Models;

namespace TallyJ.Controllers
{
  public class BallotsController : BaseController
  {
    public ActionResult Index()
    {
      return UserSession.CurrentElection.IsSingleNameElection.AsBool()
               ? View("BallotSingle", new BallotSingleModel())
               : View("BallotNormal", new BallotNormalModel());
    }

    public JsonResult SaveSingleNameVote(int pid, int vid, int count)
    {
      return new BallotSingleModel().SaveVote(pid, vid, count);
    }
    public JsonResult DeleteSingleNameVote(int vid)
    {
      return new BallotSingleModel().DeleteVote(vid);
    }
  }
}