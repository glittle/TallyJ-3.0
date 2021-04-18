using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.CoreModels;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class MiscBallotTests
  {
    [TestMethod]
    public void OnlineRawVote_Parse1()
    {
      var v = new OnlineRawVote("First Last");
      v.First.ShouldEqual("First");
      v.Last.ShouldEqual("Last");
    }
    [TestMethod]
    public void OnlineRawVote_Parse2()
    {
      var v = new OnlineRawVote("Last, First");
      v.First.ShouldEqual("First");
      v.Last.ShouldEqual("Last");
    }
    [TestMethod]
    public void OnlineRawVote_Parse3()
    {
      var text = "Last, de First";
      var v = new OnlineRawVote(text);
      v.First.ShouldEqual("");
      v.Last.ShouldEqual("");
      v.OtherInfo.ShouldEqual(text);
    }
  }
}