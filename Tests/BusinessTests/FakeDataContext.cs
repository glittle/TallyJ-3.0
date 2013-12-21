using TallyJ.EF;

namespace Tests.BusinessTests
{
  public class FakeDataContext : TallyJ2dEntities
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