using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Linq;
using EntityFramework.BulkInsert.Extensions;

namespace TallyJ.EF
{
  public partial class TallyJEntities : ITallyJDbContext
  {
    public TallyJEntities(DbConnection connection)
      : base(connection, true)
    {
      Database.CommandTimeout = 180; // default 30 seconds is too short
    }
    public void BulkInsert<T>(IEnumerable<T> entities)
    {
      this.BulkInsert(entities, 500);
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
      ((IObjectContextAdapter)(this)).ObjectContext.Detach(entity);
    }

    public long CurrentRowVersion()
    {
      return Database.SqlQuery<long>("select convert(bigint, @@DBTS)").First();
    }
  }

  public interface IDbContext
  {
    int SaveChanges();
    void Detach(object entity);
  }
}