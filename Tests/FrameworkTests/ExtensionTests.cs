using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.EF;
using TallyJ.Models;
using TallyJ.Code;

namespace Tests.FrameworkTests
{
  [TestClass]
  public class ExtensionTests
  {
    [TestMethod]
    public void JoinedAsStringTest1()
    {
      var source = new[]
                     {
                       "A",
                       "B",
                       "",
                       "D"
                     };
      source.JoinedAsString().ShouldEqual("ABD");
      
      source.JoinedAsString(",").ShouldEqual("A,B,,D");
      
      source.JoinedAsString(",", true).ShouldEqual("A,B,D");
      
      source.JoinedAsString(",", "<", ">", true).ShouldEqual("<A>,<B>,<D>");
    }
  }
}
