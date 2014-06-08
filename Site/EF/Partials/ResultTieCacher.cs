using System.Linq;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class ResultTieCacher : CacherBase<ResultTie>
  {
    public override IQueryable<ResultTie> MainQuery()
    {
      var currentElectionGuid = UserSession.CurrentElectionGuid;
      return CurrentDb.ResultTie.Where(p => p.ElectionGuid == currentElectionGuid);
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