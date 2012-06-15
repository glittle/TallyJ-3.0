using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class RollCallModel : DataConnectedModel
  {
    public RollCallModel()
    {
      IncludeAbsentees = true;
    }

    private List<Person> _people;

    public long LastVersionNum
    {
      get { return PeopleInCurrentElection().Max(p => p.C_RowVersionInt).AsLong(); }
    }

    public bool IncludeAbsentees { get; set; }

    public List<Person> PeopleInCurrentElection()
    {
      {
        if (_people == null)
        {
          var peopleInCurrentElection = PeopleInCurrentElectionQuery();
          _people = peopleInCurrentElection.ToList();
        }
        return _people;
      }
    }

    private IQueryable<Person> PeopleInCurrentElectionQuery()
    {
      var peopleInCurrentElection =
        Db.People.Where(p => p.ElectionGuid == UserSession.CurrentElectionGuid);
      // && p.VotingLocationGuid == UserSession.CurrentLocationGuid

      peopleInCurrentElection = IncludeAbsentees
                                  ? peopleInCurrentElection.Where(p => !string.IsNullOrEmpty(p.VotingMethod))
                                  : peopleInCurrentElection.Where(p => p.VotingMethod == VotingMethodEnum.InPerson);
      return peopleInCurrentElection;
    }

    public IEnumerable<object> Voters(int numBlanksBefore = 2, int numBlanksAfter = 6)
    {
      return PersonLines(PeopleInCurrentElection(), numBlanksBefore, numBlanksAfter, IncludeAbsentees);
    }

    /// <Summary>Only those listed</Summary>
    public IEnumerable<object> PersonLines(List<Person> people, int numBlanksBefore, int numBlanksAfter,
                                           bool includeAbsentees)
    {
      var before = new List<Person>();
      var after = new List<Person>();
      while (numBlanksBefore > 0)
      {
        before.Add(new Person { C_RowId = 0 - numBlanksBefore, C_FullName = "&nbsp;", VotingMethod = "&nbsp;" });
        numBlanksBefore--;
      }
      var offset = 0;
      const int firstBlankAfter = -100;
      while (numBlanksAfter > 0)
      {
        after.Add(new Person { C_RowId = firstBlankAfter + offset++, C_FullName = "&nbsp;", VotingMethod = "&nbsp;" });
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
                           FullName = p.C_FullNameFL,
                           VotingMethod = includeAbsentees ? VotingMethodEnum.PresentAbsentTextFor(p.VotingMethod) : "",
                           Pos = ++i,
                           EnvNum = p.VotingMethod.DefaultTo(VotingMethodEnum.InPerson) == VotingMethodEnum.InPerson ? null : p.EnvNum
                         });
    }

    public object GetMorePeople(long stamp, out long newStamp)
    {
      var peopleInCurrentElection = PeopleInCurrentElectionQuery().Where(p => p.C_RowVersionInt > stamp).ToList();
      if (peopleInCurrentElection.Count == 0)
      {
        newStamp = 0;
        return null;
      }

      newStamp = peopleInCurrentElection.Max(p => p.C_RowVersionInt).AsLong();

      var personLines = PersonLines(peopleInCurrentElection, 0, 0, IncludeAbsentees);

      return TemplateLoader.File.RollCallLine.FilledWithEach(personLines);
    }
  }
}