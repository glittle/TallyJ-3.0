using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Helpers;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class MetaphoneTests
  {
    [TestMethod]
    public void Match1()
    {
      "Stephen".GenerateDoubleMetaphone().ShouldEqual("Steven".GenerateDoubleMetaphone());
      "Little".GenerateDoubleMetaphone().ShouldEqual("Lyttle".GenerateDoubleMetaphone());

      "Mehri".GenerateDoubleMetaphone().ShouldEqual("Mary".GenerateDoubleMetaphone());

    }

    [TestMethod]
    public void NotMatch1()
    {
      "Smith".GenerateDoubleMetaphone().ShouldNotEqual("Jones".GenerateDoubleMetaphone());
    }
  }
}