using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace TallyJ.EF
{
  public partial class TallyJ2dEntities
  {
    public TallyJ2dEntities(DbConnection connection)
      : base(connection, true)
    {
    }

    /// <summary>
    /// Is this context a testing construct, not the real database?
    /// </summary>
    public virtual bool IsFaked
    {
      get { return false; }
    }

    public void Detach(object entity)
    {
      ((IObjectContextAdapter) (this)).ObjectContext.Detach(entity);
    }

    public long CurrentRowVersion()
    {
      return Database.SqlQuery<int>("select convert(bigint, @@DBTS)").First();
    }
  }

  public interface IDbContext
  {
    int SaveChanges();
    void Detach(object entity);
  }
}