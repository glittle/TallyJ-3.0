using Site.EF;

namespace Site.Code.Data
{
	public interface IDbContextFactory
	{
		tallyj2dEntities DbContext
		{
			get;
		}
	}
}