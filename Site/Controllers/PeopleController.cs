using System.Data.Objects.SqlClient;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;

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

    public JsonResult GetPeople(string search, bool includeInelligible = false)
    {
      var currentElection = UserSession.CurrentElection;
      if (currentElection == null)
      {
        return new
                 {
                   Error = "Election not selected"
                 }.AsJsonResult();
      }

      var parts = search.Split(new[] { ' ', '-', '\'' }, 2);
      var numParts = parts.Length;

      const int max = 25;
      var part0 = parts[0];
      var part1 = numParts > 1 ? parts[1] : part0;

      var persons = DbContext.People
        .Where(p => p.ElectionGuid == currentElection.ElectionGuid)
        .Where(p => includeInelligible || p.IneligibleReasonGuid == null)
        .Where(p => p.CombinedInfo.Contains(part0)
                     && p.CombinedInfo.Contains(part1)
                     || p.CombinedInfo.Contains(SqlFunctions.SoundCode(part0))
                    )
        .Take(max + 1)
        .ToList();

      return new
               {
                 People = persons
                   .OrderBy(p => p.LastName)
                   .ThenBy(p => p.FirstName)
                   .Take(max)
                   .Select(p => new
                                  {
                                    p.C_RowId,
                                    Name = "{0} {1}".FilledWith(p.FirstName, p.LastName),
                                  }),
                 MoreFound = persons.Count > max ? "More than {0} matches".FilledWith(max) : ""
               }
        .AsJsonResult();
    }
  }
}