using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
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
        people = new PersonCacher(Db).AllForThisElection.Select(p => PeopleModel.PersonForList(p, isSingleNameElection, votes)),
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

      var rnd = new Random();

      var useRandom = currentElection.RandomizeVotersList;

      var query = new PersonCacher(Db).AllForThisElection.AsQueryable();
      if (!useRandom)
      {
        query = query.OrderBy(p => p.FullName);
      }

      var allForThisElection = query.ToList();
      var nonRandom = 1;
      var maxRnd = allForThisElection.Count * 100;

      return new
      {
        people = allForThisElection
          .Where(p => p.CanReceiveVotes.AsBoolean())
          .Select(p =>
          {
            // var irg = p.IneligibleReasonGuid;
            // string descriptionFor = null;

            // if (irg != null)
            // {
            // if (p.CanReceiveVotes.GetValueOrDefault())
            // {
            //   // if they can receive votes, ignore any other status they may have (e.g. not a delegate)
            //   irg = null;
            // }

            // if (irg != null)
            // {
            //   descriptionFor = IneligibleReasonEnum.DescriptionFor(irg.Value);
            // }
            // }

            return new
            {
              Id = p.C_RowId,
              Name = p.FullName,
              // IRG = descriptionFor,
              p.OtherInfo,
              p.Area,
              sort = useRandom ? rnd.Next(maxRnd) : nonRandom++
            };
          })
          .OrderBy(p => p.sort)
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