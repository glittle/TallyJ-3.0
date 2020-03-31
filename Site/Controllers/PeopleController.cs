using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  public class PeopleController : BaseController
  {

    /// <summary>
    /// Need index just for making reference easier
    /// </summary>
    /// <returns></returns>
    [AllowTellersInActiveElection]
    public ActionResult Index()
    {
      return null;
    }

    [AllowTellersInActiveElection]
    public JsonResult GetAll()
    {
      var currentElection = UserSession.CurrentElection;
      if (currentElection == null)
      {
        return new
        {
          Error = "Election not selected"
        }.AsJsonResult();
      }

      var isSingleNameElection = currentElection.IsSingleNameElection;
      var votes = new VoteCacher(Db).AllForThisElection;

      return new
      {
        people = new PersonCacher(Db).AllForThisElection.Select(p => new
        {
          Id = p.C_RowId,
          //p.PersonGuid,
          Name = p.FullNameFL,
          p.Area,
          p.Email,
          p.Phone,
          V = (p.CanReceiveVotes.AsBoolean() ? "1" : "0") + (p.CanVote.AsBoolean() ? "1" : "0"),
          IRG = p.IneligibleReasonGuid,
          NumVotes = isSingleNameElection
            ? votes.Where(v => v.PersonGuid == p.PersonGuid).Sum(v => v.SingleNameElectionCount).AsInt()
            : votes.Count(v => v.PersonGuid == p.PersonGuid)
        }),
        lastVid = votes.Any() ? votes.Max(v => v.C_RowId) : 0
      }.AsJsonResult();
    }

    [AllowVoter]
    public JsonResult GetForVoter()
    {
      var currentElection = UserSession.CurrentElection;
      if (currentElection == null)
      {
        return new
        {
          Error = "Election not selected"
        }.AsJsonResult();
      }

      var selectionProcess = currentElection.OnlineSelectionProcess.AsEnum(OnlineSelectionProcessEnum.Random);
      switch (selectionProcess)
      {
        case OnlineSelectionProcessEnum.List:
        case OnlineSelectionProcessEnum.Both:
          // okay
          break;
        default:
          // empty list
          return new
          {
            people = new List<string>()
          }.AsJsonResult();
      }

      return new
      {
        people = new PersonCacher(Db)
          .AllForThisElection
          .Select(p => new
          {
            Id = p.C_RowId,
            Name = p.FullNameFL,
            IRG = IneligibleReasonEnum.DescriptionFor(p.IneligibleReasonGuid.GetValueOrDefault()),
            p.OtherInfo,
            p.Area
          }),
      }.AsJsonResult();
    }

    //public JsonResult GetPeople(string search, bool includeMatches = false, bool forBallot = true)
    //{
    //  var currentElection = UserSession.CurrentElection;
    //  if (currentElection == null)
    //  {
    //    return new
    //    {
    //      Error = "Election not selected"
    //    }.AsJsonResult();
    //  }

    //  var model = new PeopleSearchModel();
    //  return model.Search2(search, includeMatches, forBallot);
    //}

    [AllowTellersInActiveElection]
    public JsonResult GetDetail(int id)
    {
      var model = new PeopleModel();
      return model.DetailsFor(id);
    }

  }
}