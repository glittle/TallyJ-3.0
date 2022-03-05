using System;
using System.Collections.Generic;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using System.Linq;
using TallyJ.EF;
using TallyJ.Code.Enumerations;

namespace TallyJ.Controllers
{
  [AllowTellersInActiveElection]
  public class BallotsController : BaseController
  {
    public ActionResult Index()
    {
      var locationId = Request.QueryString["l"].AsInt();
      if (locationId != 0 && (UserSession.CurrentLocation == null || locationId != UserSession.CurrentLocation.C_RowId))
      {
        // switch to location, if allowed
        var switched = new ComputerModel().MoveCurrentComputerIntoLocation(locationId);
        if (!switched)
        {
          return RedirectToAction("ElectionList", "Dashboard");
        }
      }

      var isSingle = UserSession.CurrentElection.IsSingleNameElection;
      var ballotModel = CurrentBallotModel;

      var ballotId = Request.QueryString["b"].AsInt();
      if (ballotId == 0)
      {
        if (isSingle)
        {
          ballotModel.GetCurrentBallot();
        }
      }
      else
      {
        ballotModel.SetAsCurrentBallot(ballotId);
        var ballot = ballotModel.GetCurrentBallot(false);

        var filter = UserSession.CurrentBallotFilter;
        if (filter.HasContent() && ballot != null && ballot.ComputerCode != filter)
        {
          UserSession.CurrentBallotFilter = ballot.ComputerCode;
        }
      }

      return isSingle ? View("BallotSingle", ballotModel) : View("BallotNormal", ballotModel);
    }

    public ActionResult Reconcile()
    {
      return View(new PeopleModel());
    }


    public ActionResult SortBallots()
    {
      return View(new PeopleModel());
    }

    public JsonResult BallotsForLocation(int id)
    {
      var peopleModel = new PeopleModel();
      return new
      {
        Ballots = peopleModel.BallotSources(id),
        Deselected = peopleModel.Deselected()
      }.AsJsonResult();
    }

    public JsonResult SaveVote(int pid, int vid, string invalid = "", int count = 0, int lastVid = 0, bool verifying = false)
    {
      var invalidGuid = invalid.AsNullableGuid();
      return CurrentBallotModel.SaveVote(pid, vid, invalidGuid, lastVid, count, verifying);
    }

    public JsonResult DeleteVote(int vid)
    {
      return CurrentBallotModel.DeleteVote(vid);
    }

    public JsonResult NeedsReview(bool needs)
    {
      return CurrentBallotModel.SetNeedsReview(needs);
    }

    public JsonResult SwitchToBallot(int ballotId, bool refresh)
    {
      return CurrentBallotModel.SwitchToBallotAndGetInfo(ballotId, refresh).AsJsonResult();
    }

    public JsonResult UpdateLocationStatus(int id, string status)
    {
      return ContextItems.LocationModel.UpdateStatus(id, status);
    }

    //public JsonResult UpdateBallotStatus(string status)
    //{
    //  return new
    //           {
    //             Status = status,
    //             Updated = false
    //           }.AsJsonResult();
    //  //return CurrentBallotModel.UpdateBallotStatus(status);
    //}

    public JsonResult UpdateLocationInfo(string info)
    {
      return ContextItems.LocationModel.UpdateLocationInfo(info);
    }

    public JsonResult GetLocationInfo()
    {
      if (UserSession.CurrentLocation == null)
      {
        return new { Message = "Must select your location first!" }.AsJsonResult();
      }

      var locationModel = ContextItems.LocationModel;

      //      if (UserSession.CurrentElection.IsSingleNameElection)
      //      {
      //        return new
      //        {
      //          Location = locationModel.CurrentBallotLocationInfo(),
      //          BallotInfo = CurrentBallotModel.CurrentBallotInfo(),
      //          Ballots = CurrentBallotModel.CurrentBallotsInfoList()
      //        }.AsJsonResult();
      //      }

      return new
      {
        Location = locationModel.CurrentBallotLocationInfo(),
        BallotInfo = CurrentBallotModel.CurrentBallotInfo(),
        Ballots = CurrentBallotModel.CurrentBallotsInfoList()
      }.AsJsonResult();
    }

    public JsonResult UpdateLocationCollected(int numCollected)
    {
      return ContextItems.LocationModel.UpdateNumCollected(numCollected);
    }

    public JsonResult RefreshBallotsList()
    {
      return CurrentBallotModel.CurrentBallotsInfoList(true).AsJsonResult();
    }

    public JsonResult ChangeBallotFilter(string code)
    {
      UserSession.CurrentBallotFilter = code;
      return CurrentBallotModel.CurrentBallotsInfoList().AsJsonResult();
    }

    public JsonResult SortVotes(List<int> idList)
    {
      if (UserSession.CurrentElectionStatus == ElectionTallyStatusEnum.Finalized)
      {
        return new { Message = UserSession.FinalizedNoChangesMessage }.AsJsonResult();
      }

      return CurrentBallotModel.SortVotes(idList, new VoteCacher(Db)).AsJsonResult();
    }

    public JsonResult NewBallot()
    {
      return CurrentBallotModel.StartNewBallotJson();
    }

    public JsonResult DeleteBallot()
    {
      return CurrentBallotModel.DeleteBallotJson();
    }

    private static IBallotModel CurrentBallotModel
    {
      get { return BallotModelFactory.GetForCurrentElection(); }
    }
  }
}