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

       election.BallotProcessRaw.ShouldEqual(BallotProcessEnum.Roll.ToString());

      election.BallotProcessRaw = BallotProcessEnum.RegC.ToString();

      election.BallotProcessRaw.ShouldEqual("RegC");

      election.BallotProcessRaw = BallotProcessEnum.None.ToString();
      election.BallotProcessRaw.ShouldEqual(BallotProcessEnum.None.ToString());

      election.BallotProcessRaw = null;

      // always defaults to Roll
      election.BallotProcessRaw.ShouldEqual(BallotProcessEnum.Roll.ToString());

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