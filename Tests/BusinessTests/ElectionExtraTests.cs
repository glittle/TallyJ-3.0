using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Support;
using System;
using System.Linq;
using TallyJ.EF;

namespace Tests.BusinessTests
{
  [TestClass]

  public class ElectionExtraTests {
    [TestMethod]
    public void ExtraFakeColumnsTest() {
      var election = new Election();

      election.UsePreBallot.ShouldEqual(false);

      election.UsePreBallot = true;
      election.UsePreBallot.ShouldEqual(true);

      election.OwnerLoginId.ShouldEqual("~PreB=1");
    }
  }
}