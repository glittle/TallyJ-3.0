using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class RollCallModel : DataConnectedModel
  {
    public IQueryable<Person> PeopleInCurrentElection(bool includeAbsentees)
    {
      {
        var peopleInCurrentElection =
          Db.People.Where(
            p =>
            p.ElectionGuid == UserSession.CurrentElectionGuid);
        // && p.VotingLocationGuid == UserSession.CurrentLocationGuid

        peopleInCurrentElection = includeAbsentees
                                    ? peopleInCurrentElection.Where(p => !string.IsNullOrEmpty(p.VotingMethod))
                                    : peopleInCurrentElection.Where(p => p.VotingMethod == VotingMethodEnum.InPerson);

        return peopleInCurrentElection;
      }
    }

    public IEnumerable<object> Voters(int numBlanksBefore = 4, int numBlanksAfter = 6, bool includeAbsentees = true)
    {
      return PersonLines(PeopleInCurrentElection(includeAbsentees).ToList(), numBlanksBefore, numBlanksAfter,
                         includeAbsentees);
    }

    /// <Summary>Only those listed</Summary>
    public IEnumerable<object> PersonLines(List<Person> people, int numBlanksBefore, int numBlanksAfter,
                                           bool includeAbsentees)
    {
      var before = new List<Person>();
      var after = new List<Person>();
      while (numBlanksBefore > 0)
      {
        before.Add(new Person { C_FullName = "&nbsp;", VotingMethod = "&nbsp;" });
        numBlanksBefore--;
      }
      while (numBlanksAfter > 0)
      {
        after.Add(new Person { C_FullName = "&nbsp;", VotingMethod = "&nbsp;" });
        numBlanksAfter--;
      }
      var i = 0;
      return
        before.Concat(people
                        .OrderBy(p => p.LastName)
                        .ThenBy(p => p.FirstName)).Concat(after)
          .Select(p => new
                         {
                           PersonId = p.C_RowId,
                           FullName = p.FirstName + " " + p.LastName,
                           VotingMethod = includeAbsentees ? VotingMethodEnum.TextFor(p.VotingMethod) : "",
                           Pos = ++i,
                           EnvNum = p.VotingMethod.DefaultTo(VotingMethodEnum.InPerson) == VotingMethodEnum.InPerson ? null : p.EnvNum
                         });
    }
  }
}