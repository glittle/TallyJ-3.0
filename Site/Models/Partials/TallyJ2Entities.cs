using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.EntityClient;

namespace TallyJ.Models
{
    public partial class TallyJ2dContext : DbContext, IDbContext
  {
    public TallyJ2dContext(DbConnection connection)
      : base(connection, true)
    {
    }

    public void Detach(object entity)
    {
      ((IObjectContextAdapter)(this)).ObjectContext.Detach(entity);
    }

  }

  public interface IDbContext
  {
    int SaveChanges();
    void Detach(object entity);
  }
}