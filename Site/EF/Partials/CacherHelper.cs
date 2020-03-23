using EntityFramework.Caching;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  public class CacherHelper
  {
    /// <summary>
    ///   Remove all cached data for this election
    /// </summary>
    public void DropAllCachesForThisElection()
    {
      //if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;
      var numExpired = CacheManager.Current.Expire(UserSession.CurrentElectionGuid.ToString());
    }
  }
}