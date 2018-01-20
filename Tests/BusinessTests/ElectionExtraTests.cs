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
    public void ExtraFakeTest() {
       var election = new Election();

       election.BallotProcess.ShouldEqual(null);

      election.BallotProcess = BallotProcessKey.Reg.ToString();

      election.BallotProcess.ShouldEqual("Reg");

      election.BallotProcess = BallotProcessKey.None.ToString();
      election.BallotProcess.ShouldEqual(BallotProcessKey.None.ToString());

      election.BallotProcess = null;
      election.BallotProcess.ShouldEqual(null);

      election.OwnerLoginId.ShouldEqual(null);

    }


    //public void ExtraFakeColumnsTest() {
    //  var election = new Election();

    //  election.UsePreBallot.ShouldEqual(false);

    //  election.UsePreBallot = true;
    //  election.OwnerLoginId.ShouldEqual("~PreB=1");

    //  election.UsePreBallot.ShouldEqual(true);

    //  election.Test2.ShouldEqual(null);
    //  election.Test2 = "Hello!";

    //  election.Test2.ShouldEqual("Hello!");

    //  election.UsePreBallot.ShouldEqual(true);

    //  election.UsePreBallot = false;
    //  election.UsePreBallot.ShouldEqual(false);

    //  election.Test2.ShouldEqual("Hello!");

    //  election.Test2 = "";

    //  election.OwnerLoginId.ShouldEqual(null);
    //}
  }
}