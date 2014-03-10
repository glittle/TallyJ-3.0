using EntityFramework.Caching;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.EF
{
  public class CacherHelper
  {
    /// <summary>
    ///   Remove all cached data for this election
    /// </summary>
    public void DropAllCachesForThisElection()
    {
      if (UnityInstance.Resolve<IDbContextFactory>().DbContext.IsFaked) return;
      CacheManager.Current.Expire(UserSession.CurrentElectionGuid.ToString());
    }
  }
}