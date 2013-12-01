using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace TallyJ.Code
{
  public abstract class DataConnectedModel
  {

    private TallyJ2dEntities _db;

    /// <summary>
    ///     Access to the database
    /// </summary>
    protected TallyJ2dEntities Db
    {
      get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
    }
  }
}