using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class PeopleCacher : CacherBase<Person>
  {
    protected override IQueryable<Person> MainQuery(TallyJ2dEntities db)
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      return db.Person.Where(p => p.ElectionGuid == currentElectionGuid);
    }
  }
}