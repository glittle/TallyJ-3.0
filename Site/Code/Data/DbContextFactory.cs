using System.Configuration;
using System.Data.EntityClient;
using TallyJ.EF;

namespace TallyJ.Code.Data
{
	public class DbContextFactory : IDbContextFactory
	{
		private tallyj2dEntities _db;

		public tallyj2dEntities DbContext
		{
			get
			{
				if (_db != null)
					return _db;

				var cnString = ConfigurationManager.ConnectionStrings["MainConnection"].ConnectionString;
				var final = new EntityConnection("metadata=res://*;provider=System.Data.SqlClient;provider connection string='" + cnString + "'");
				return _db = new tallyj2dEntities(final);
			}
		}
	}
}
