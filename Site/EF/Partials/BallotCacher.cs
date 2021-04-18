using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code.Session;
using TallyJ.CoreModels;

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
    public BallotCacher() : base(UserSession.GetNewDbContext)
    {
    }

    public Ballot GetByComputerCode()
    {
      return AllForThisElection.FirstOrDefault(b => b.LocationGuid == UserSession.CurrentLocationGuid && b.ComputerCode == UserSession.CurrentComputerCode);
    }

    // public IEnumerable<Ballot> BallotsFromOnline()
    // {
    //   return AllForThisElection.Where(t => t.ComputerCode == ComputerModel.ComputerCodeForOnline);
    // }

    protected override object LockCacheBaseObject
    {
      get
      {
        return _lockObject ?? (_lockObject = new object());
      }
    }
  }
}