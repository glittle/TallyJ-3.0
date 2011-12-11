using System.Data.Common;
using System.Data.Entity;
using System.Data.EntityClient;

namespace TallyJ.EF
{
	public partial class TallyJ2Entities : DbContext
	{
		public TallyJ2Entities(DbConnection connection)
			: base(connection, true)
		{
		}
	}
}