using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels;

namespace TallyJ.Controllers
{
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