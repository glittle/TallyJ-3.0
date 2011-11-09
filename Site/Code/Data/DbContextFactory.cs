using System.Configuration;
using System.Data.EntityClient;
using TallyJ.EF;

namespace TallyJ.Code.Data
{
	public class DbContextFactory : IDbContextFactory
	{
		private TallyJ2Entities _db;

		public TallyJ2Entities DbContext
		{
			get
			{
				if (_db != null)
					return _db;

        var cnString = ConfigurationManager.ConnectionStrings["MainConnection"].ConnectionString + ";MultipleActiveResultSets=True";
				var final = new EntityConnection("metadata=res://*;provider=System.Data.SqlClient;provider connection string='" + cnString + "'");
				return _db = new TallyJ2Entities(final.ConnectionString);
			}
		}
	}
}
