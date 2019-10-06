using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace Tests.BusinessTests
{
  public class FakeDbContextFactory : IDbContextFactory
  {
    public ITallyJDbContext GetNewDbContext
    {
      get {
        return UnityInstance.Resolve<ITallyJDbContext>() ?? new TestDbContext();
      }
    }

    public void CloseAll()
    {
      throw new System.NotImplementedException();
    }
  }
}