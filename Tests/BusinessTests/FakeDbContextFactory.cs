using TallyJ.Code.Data;
using TallyJ.EF;

namespace Tests.BusinessTests
{
  public class FakeDbContextFactory : IDbContextFactory
  {
    public TallyJ2dEntities DbContext
    {
      get { return new FakeDataContext(); }
    }

  }
}