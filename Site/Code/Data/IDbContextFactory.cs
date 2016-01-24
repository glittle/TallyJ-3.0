using TallyJ.EF;

namespace TallyJ.Code.Data
{
	public interface IDbContextFactory
	{
		ITallyJDbContext DbContext
		{
			get;
		}
	}
}