using EntityFramework.BulkInsert.Extensions;
using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace TallyJ.Code
{
  public abstract class DataConnectedModel
  {

    private ITallyJDbContext _db;

    /// <summary>
    ///     Access to the database
    /// </summary>
    protected ITallyJDbContext Db
    {
      get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
      set { _db = value; }
    }

    public long LastRowVersion
    {
      get
      {
        var single = Db.CurrentRowVersion();
        return single;
      }
    }
  }
}