using TallyJ.Models;

namespace Tests.BusinessTests
{
  public class FakeDataContext : TallyJ2dContext
  {
    public override bool IsFaked
    {
      get { return true; }
    }

    public override int SaveChanges()
    {
      // okay
      return 0;
    }
  }
}