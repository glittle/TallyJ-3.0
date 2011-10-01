using TallyJ.EF;

namespace TallyJ.Code.Data
{
	public interface IDbContextFactory
	{
		tallyj2dEntities DbContext
		{
			get;
		}
	}
}