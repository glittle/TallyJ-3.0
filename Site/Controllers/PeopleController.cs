using System;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
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
    [AllowGuestsInActiveElection]
    public ActionResult Index()
    {
      return null;
    }

    [AllowGuestsInActiveElection]
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
          V = (p.CanReceiveVotes.GetValueOrDefault() ? "1" : "0") + (p.CanVote.GetValueOrDefault() ? "1" : "0"),
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

      return new
      {
        people = new PersonCacher(Db)
          .AllForThisElection
          .Select(p => new
          {
            Id = p.C_RowId,
            Name = p.FullNameFL,
            p.Area,
            IRG = p.IneligibleReasonGuid,
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

    [AllowGuestsInActiveElection]
    public JsonResult GetDetail(int id)
    {
      var model = new PeopleModel();
      return model.DetailsFor(id);
    }

  }
}