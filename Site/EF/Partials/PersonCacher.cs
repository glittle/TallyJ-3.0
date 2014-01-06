using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class PersonCacher : CacherBase<Person>
  {
    protected override IQueryable<Person> MainQuery()
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      return CurrentDb.Person.Where(p => p.ElectionGuid == currentElectionGuid);
    }
  
    protected override void ItemChanged()
    {
      new ResultSummaryCacher().VoteOrPersonChanged();
    }
  }
}