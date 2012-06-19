using System.Data.Common;
using System.Data.Entity;
using System.Data.EntityClient;

namespace TallyJ.EF
{
	public partial class TallyJ2dContext : DbContext, IDbContext
	{
    public TallyJ2dContext(DbConnection connection)
			: base(connection, true)
		{
		}
	}

  public interface IDbContext
  {
    int SaveChanges();
  }
}