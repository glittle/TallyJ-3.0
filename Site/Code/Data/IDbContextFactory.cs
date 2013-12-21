using TallyJ.EF;
using TallyJ.EF;

namespace TallyJ.Code.Data
{
	public interface IDbContextFactory
	{
		TallyJ2dEntities DbContext
		{
			get;
		}
	}
}