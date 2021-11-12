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
      v.First.ShouldEqual("de First");
      v.Last.ShouldEqual("Last");
      v.OtherInfo.ShouldEqual(text);
    }
    [TestMethod]
    public void OnlineRawVote_Parse4()
    {
      var text = "John Smith, de First";
      var v = new OnlineRawVote(text);
      v.First.ShouldEqual("de First");
      v.Last.ShouldEqual("John Smith");
      v.OtherInfo.ShouldEqual(text);
    }
    [TestMethod]
    public void OnlineRawVote_Parse5()
    {
      var text = "de First John Smith";
      var v = new OnlineRawVote(text);
      v.First.ShouldEqual("de First John");
      v.Last.ShouldEqual("Smith");
      v.OtherInfo.ShouldEqual(text);
    }
  }
}