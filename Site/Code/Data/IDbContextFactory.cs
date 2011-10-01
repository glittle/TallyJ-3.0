using TallyJ.EF;

namespace TallyJ.Code.Data
{
	public interface IDbContextFactory
	{
		TallyJ2Entities DbContext
		{
			get;
		}
	}
}