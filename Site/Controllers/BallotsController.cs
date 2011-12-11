using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.Models;

namespace TallyJ.Controllers
{
  public class BallotsController : BaseController
  {
    ///// <Summary>switch to location l, show ballot b</Summary>
    //public ActionResult Index(int l, int b)
    //{
    //  if (UserSession.CurrentLocation == null || l != UserSession.CurrentLocation.C_RowId)
    //  {
    //    // switch to location, if allowed
    //    var switched = new ComputerModel().AddCurrentComputerIntoLocation(l);
    //    if (!switched)
    //    {
    //      return RedirectToAction("ChooseElection", "Dashboard");
    //    }
    //  }

    //  return Index(b);
    //}

    ///// <Summary>Show the desired ballot</Summary>
    //public ActionResult Index(int b)
    //{
    //  SessionKey.CurrentBallotId.SetInSession(b);
    //  return Index();
    //}

    public ActionResult Index()
    {
      var locationId = Request.QueryString["l"].AsInt();
      if (locationId != 0 && (UserSession.CurrentLocation == null || locationId != UserSession.CurrentLocation.C_RowId))
      {
        // switch to location, if allowed
        var switched = new ComputerModel().AddCurrentComputerIntoLocation(locationId);
        if (!switched)
        {
          return RedirectToAction("ChooseElection", "Dashboard");
        }
      }

      var isSingle = UserSession.CurrentElection.IsSingleNameElection.AsBool();
      var ballotModel = BallotModelFactory.GetForCurrentElection();

      var ballotId = Request.QueryString["b"].AsInt();
      if (ballotId == 0)
      {
        if (isSingle)
        {
          ballotModel.GetCurrentBallotInfo(false);
        }
      }
      else
      {
        ballotModel.SetAsCurrentBallot(ballotId);
      }

      return isSingle ? View("BallotSingle", ballotModel) : View("BallotNormal", ballotModel);
    }

    public JsonResult SaveSingleNameVote(int pid, int vid, int count, int invalid = 0)
    {
      return BallotModelFactory.GetForCurrentElection().SaveVote(pid, vid, count, invalid);
    }

    public JsonResult DeleteSingleNameVote(int vid)
    {
      return BallotModelFactory.GetForCurrentElection().DeleteVote(vid);
    }

    public JsonResult SwitchToBallot(int ballotId)
    {
      return BallotModelFactory.GetForCurrentElection().SwitchToBallotJson(ballotId);
    }

    public JsonResult UpdateLocationStatus(int id, string status)
    {
      return new LocationModel().UpdateStatus(id, status);
    }
    public JsonResult UpdateLocationInfo(string info)
    {
      return new LocationModel().UpdateContactInfo(info);
    }
    public JsonResult UpdateLocationCollected(int numCollected)
    {
      return new LocationModel().UpdateNumCollected(numCollected);
    }
  }
}