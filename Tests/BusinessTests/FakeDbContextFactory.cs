using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace Tests.BusinessTests
{
  public class FakeDbContextFactory : IDbContextFactory
  {
    public ITallyJDbContext DbContext
    {
      get {
        return UnityInstance.Resolve<ITallyJDbContext>() ?? new TestDbContext();
      }
    }

  }
}