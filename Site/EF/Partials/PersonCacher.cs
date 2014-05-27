using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class PersonCacher : CacherBase<Person>
  {
    public override IQueryable<Person> MainQuery()
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      return CurrentDb.Person.Where(p => p.ElectionGuid == currentElectionGuid);
    }
  
    protected override void ItemChanged()
    {
      new ResultSummaryCacher().VoteOrPersonChanged();
    }

    private static object _lockObject;
    protected override object LockCacheBaseObject
    {
      get
      {
        return _lockObject ?? (_lockObject = new object());
      }
    }

  }
}