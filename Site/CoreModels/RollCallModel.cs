using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class RollCallModel : DataConnectedModel
  {
    private List<Person> _people;

    public RollCallModel()
    {
      IncludeAbsentees = true;
    }

    public long LastVersionNum
    {
      get
      {
        var peopleInCurrentElection = PeopleInCurrentElection();

        return peopleInCurrentElection.Count == 0 ? 0 : peopleInCurrentElection.Max(p => p.C_RowVersionInt).AsLong();
      }
    }

    public bool IncludeAbsentees { get; set; }

    public List<Person> PeopleInCurrentElection()
    {
      {
        return _people ?? (_people = PeopleInCurrentElectionQuery().ToList());
      }
    }

    private IEnumerable<Person> PeopleInCurrentElectionQuery()
    {
      var peopleInCurrentElection = new PersonCacher(Db).AllForThisElection;
      // && p.VotingLocationGuid == UserSession.CurrentLocationGuid
      var withAbsentees = new[]
      {
        VotingMethodEnum.InPerson,
        VotingMethodEnum.DroppedOff,
        VotingMethodEnum.MailedIn,
        //VotingMethodEnum.Registered, -- not registered (should not be used with RollCall)
        VotingMethodEnum.CalledIn,
        VotingMethodEnum.Online,
        VotingMethodEnum.Custom1,
        VotingMethodEnum.Custom2,
        VotingMethodEnum.Custom3,
      }.Select(vm => vm.Value).ToList();

      return IncludeAbsentees
        ? peopleInCurrentElection.Where(p => withAbsentees.Contains(p.VotingMethod)).ToList()
        : peopleInCurrentElection.Where(p => p.VotingMethod == VotingMethodEnum.InPerson).ToList();
    }

    public IEnumerable<object> Voters(int numBlanksBefore = 3, int numBlanksAfter = 6)
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
        before.Add(new Person { C_RowId = 0 - numBlanksBefore, LastName = "&nbsp;", VotingMethod = "BLANK" });
        numBlanksBefore--;
      }
      var offset = 0;
      const int firstBlankAfter = -100;
      while (numBlanksAfter > 0)
      {
        after.Add(new Person { C_RowId = firstBlankAfter + offset++, LastName = "&nbsp;", VotingMethod = "BLANK" });
        numBlanksAfter--;
      }
      var locationModel = new LocationModel();
      //      var currentElection = UserSession.CurrentElection;
      var i = 0;
      return
        before.Concat(people
          .OrderBy(p => p.LastName)
          .ThenBy(p => p.FirstName)).Concat(after)
          .Select(p => new
          {
            PersonId = p.C_RowId,
            FullName = p.C_FullName, //.FullNameFL,
            Area = p.Area,
            TS = p.C_RowVersionInt,
            Loc = locationModel.IdFor(p.VotingLocationGuid),
            Env = p.VotingMethod == VotingMethodEnum.Online ? null : p.EnvNum,
            VM = p.VotingMethod,
            Pos = ++i
          });
    }

    public IEnumerable<object> GetMorePeople(long stamp, out long newStamp)
    {
      var newPeopleInCurrentElection = PeopleInCurrentElectionQuery().Where(p => p.C_RowVersionInt > stamp).ToList();
      if (newPeopleInCurrentElection.Count == 0)
      {
        newStamp = 0;
        return null;
      }

      newStamp = newPeopleInCurrentElection.Max(p => p.C_RowVersionInt).AsLong();

      return PersonLines(newPeopleInCurrentElection, 0, 0, IncludeAbsentees);
    }

    //public class PersonVotingMethodInfo
    //{
    //  public int PersonId { get; set; }
    //  public string FullName { get; set; }
    //  public long? TS { get; set; }
    //  public string VotingMethod { get; set; }
    //  public int Pos { get; set; }
    //  public int Loc { get; internal set; }
    //  public string VM { get; internal set; }
    //}
  }
}