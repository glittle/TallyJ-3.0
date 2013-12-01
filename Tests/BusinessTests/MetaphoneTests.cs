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

      "Glenn".GenerateDoubleMetaphone().ShouldEqual("Glen".GenerateDoubleMetaphone());
      
      "Tom".GenerateDoubleMetaphone().ShouldEqual("Thom".GenerateDoubleMetaphone());

      // "Shervin".GenerateDoubleMetaphone().ShouldEqual("Sherwin".GenerateDoubleMetaphone());

    }

    [TestMethod]
    public void NotMatch1()
    {
      "Smith".GenerateDoubleMetaphone().ShouldNotEqual("Jones".GenerateDoubleMetaphone());
    }
  }
}