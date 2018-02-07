using System;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.EF;

namespace TallyJ.Controllers
{
  [AllowGuestsInActiveElection]
  public class PeopleController : BaseController
  {

    /// <summary>
    /// Need index just for making reference easier
    /// </summary>
    /// <returns></returns>
    public ActionResult Index()
    {
      return null;
    }


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
          p.PersonGuid,
          Name = p.FullNameAndArea,
          p.CanReceiveVotes,
          p.CanVote,
          p.IneligibleReasonGuid,
          RowVersion = p.C_RowVersionInt.HasValue ? p.C_RowVersionInt.Value : 0,
          Count = isSingleNameElection
            ? votes.Where(v => v.PersonGuid == p.PersonGuid).Sum(v => v.SingleNameElectionCount).AsInt()
            : votes.Count(v => v.PersonGuid == p.PersonGuid)
        })
      }.AsJsonResult();
    }

    public JsonResult GetPeople(string search, bool includeMatches = false, bool forBallot = true)
    {
      var currentElection = UserSession.CurrentElection;
      if (currentElection == null)
      {
        return new
        {
          Error = "Election not selected"
        }.AsJsonResult();
      }

      var model = new PeopleSearchModel();
      return model.Search2(search, includeMatches, forBallot);
    }

    public JsonResult GetDetail(int id)
    {
      var model = new PeopleModel();
      return model.DetailsFor(id);
    }

  }
}