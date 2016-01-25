using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class PersonCacher : CacherBase<Person>
  {
    public override IQueryable<Person> MainQuery()
    {
      return CurrentDb.Person.Where(p => p.ElectionGuid == CurrentElectionGuid);
    }
  
    protected override void ItemChanged()
    {
      new ResultSummaryCacher(CurrentDb).VoteOrPersonChanged();
    }

    private static object _lockObject;

    public PersonCacher(ITallyJDbContext dbContext) : base(dbContext) {
    }
    public PersonCacher() : base(UserSession.DbContext)
    {
    }


    protected override object LockCacheBaseObject
    {
      get
      {
        return _lockObject ?? (_lockObject = new object());
      }
    }

  }
}