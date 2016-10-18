using System;
using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class BallotCacher : CacherBase<Ballot>
  {
    public override IQueryable<Ballot> MainQuery()
    {
      return CurrentDb.Ballot
        .Join(CurrentDb.Location.Where(l => l.ElectionGuid == CurrentElectionGuid), b => b.LocationGuid, l => l.LocationGuid, (b, l) => b);
    }

    protected override void ItemChanged()
    {
      new ResultSummaryCacher(CurrentDb).VoteOrPersonChanged();
    }

    private static object _lockObject;

    public BallotCacher(ITallyJDbContext dbContext) : base(dbContext)
    {
    }
    public BallotCacher() : base(UserSession.DbContext)
    {
    }

    public Ballot GetByComputerCode()
    {
      return AllForThisElection.FirstOrDefault(t => t.ComputerCode == UserSession.CurrentComputerCode);
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