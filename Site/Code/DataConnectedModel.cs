using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace TallyJ.Code
{
  public abstract class DataConnectedModel
  {

    private ITallyJDbContext _db;

    protected DataConnectedModel()
    {
    }

    protected DataConnectedModel(ITallyJDbContext db)
    {
      _db = db;
    }

    /// <summary>
    ///     Access to the database
    /// </summary>
    protected ITallyJDbContext Db
    {
      get => _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().GetNewDbContext);
      set => _db = value;
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