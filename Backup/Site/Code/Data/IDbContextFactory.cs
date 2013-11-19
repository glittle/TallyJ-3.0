using TallyJ.Models;

namespace TallyJ.Code.Data
{
	public interface IDbContextFactory
	{
		TallyJ2dContext DbContext
		{
			get;
		}
	}
}