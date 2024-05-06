using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.CoreModels;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class MiscBallotTests
  {

    /// <summary>
    /// Parses the first and last name from the input string and assigns them to the corresponding properties.
    /// </summary>
    /// <remarks>
    /// This method creates a new instance of the OnlineRawVote class with the specified first and last name.
    /// It then asserts that the First and Last properties of the instance are equal to the provided first and last names, respectively.
    /// </remarks>
    [TestMethod]
    public void OnlineRawVote_Parse1()
    {
      var v = new OnlineRawVote("First Last");
      v.First.ShouldEqual("First");
      v.Last.ShouldEqual("Last");
    }

    /// <summary>
    /// Parses the input string containing the last and first name and assigns them to the corresponding properties.
    /// </summary>
    /// <remarks>
    /// This method creates a new instance of the OnlineRawVote class with the input string containing the last and first name.
    /// It then assigns the first name to the First property and the last name to the Last property.
    /// </remarks>
    /// <exception cref="Exception">Thrown if the input string is not in the expected format.</exception>
    [TestMethod]
    public void OnlineRawVote_Parse2()
    {
      var v = new OnlineRawVote("Last, First");
      v.First.ShouldEqual("First");
      v.Last.ShouldEqual("Last");
    }

    /// <summary>
    /// Parses the input text to extract first and last names, and other information.
    /// </summary>
    /// <param name="text">The input text containing the last name followed by the first name.</param>
    /// <remarks>
    /// This method creates an instance of the OnlineRawVote class with the provided input text.
    /// It then extracts the first and last names from the input text and assigns them to the corresponding properties of the instance.
    /// The remaining part of the input text is assigned to the OtherInfo property of the instance.
    /// </remarks>
    [TestMethod]
    public void OnlineRawVote_Parse3()
    {
      var text = "Last, de First";
      var v = new OnlineRawVote(text);
      v.First.ShouldEqual("de First");
      v.Last.ShouldEqual("Last");
      v.OtherInfo.ShouldEqual(text);
    }

    /// <summary>
    /// Parses the input text to extract first name, last name, and other information.
    /// </summary>
    /// <param name="text">The input text containing the name and other information.</param>
    /// <returns>An instance of OnlineRawVote containing the parsed first name, last name, and other information.</returns>
    /// <remarks>
    /// This method parses the input <paramref name="text"/> to extract the first name, last name, and other information.
    /// It then creates an instance of OnlineRawVote and assigns the parsed values to its properties - First, Last, and OtherInfo.
    /// </remarks>
    [TestMethod]
    public void OnlineRawVote_Parse4()
    {
      var text = "John Smith, de First";
      var v = new OnlineRawVote(text);
      v.First.ShouldEqual("de First");
      v.Last.ShouldEqual("John Smith");
      v.OtherInfo.ShouldEqual(text);
    }

    /// <summary>
    /// Parses the input text to extract the first and last name, and other information.
    /// </summary>
    /// <remarks>
    /// This method parses the input <paramref name="text"/> to extract the first and last name, and stores the remaining information in the <see cref="OtherInfo"/> property of the <see cref="OnlineRawVote"/> object.
    /// </remarks>
    /// <exception cref="AssertionException">Thrown if the parsed first name does not match the expected value.</exception>
    /// <exception cref="AssertionException">Thrown if the parsed last name does not match the expected value.</exception>
    /// <param name="text">The input text to be parsed.</param>
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