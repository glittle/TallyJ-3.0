using System.Data.Entity;

namespace TallyJ.EF
{
	public partial class TallyJ2Entities : DbContext
	{
		public TallyJ2Entities(string connectionString)
			: base(connectionString)
		{
		}
	}
}