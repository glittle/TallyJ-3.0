using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.Models;

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

    public JsonResult GetPeople(string search, bool includeInelligible = false, bool includeMatches = false)
    {
      var currentElection = UserSession.CurrentElection;
      if (currentElection == null)
      {
        return new
                 {
                   Error = "Election not selected"
                 }.AsJsonResult();
      }

      var model = new PeopleSearchModel(new PeopleModel().PeopleInCurrentElection(includeInelligible));
      return model.Search(search, includeMatches);
    }

    public JsonResult GetDetail(int id)
    {
      var model = new PeopleModel();
      return model.DetailsFor(id);
    }
  }
}