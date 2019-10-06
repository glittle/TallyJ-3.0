using TallyJ.EF;

namespace TallyJ.Code.Data
{
	public interface IDbContextFactory
	{
		ITallyJDbContext GetNewDbContext
		{
			get;
		}

    void CloseAll();
  }
}