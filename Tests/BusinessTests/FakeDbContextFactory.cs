using TallyJ.Code.Data;
using TallyJ.Models;

namespace Tests.BusinessTests
{
  public class FakeDbContextFactory : IDbContextFactory
  {
    public TallyJ2dContext DbContext
    {
      get { return new FakeDataContext(); }
    }

  }
}