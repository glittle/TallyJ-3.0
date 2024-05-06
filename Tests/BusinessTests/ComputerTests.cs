using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.CoreModels;
using System.Linq;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class ComputerTests
  {

    /// <summary>
    /// Determines the next free computer code based on the given list of existing codes.
    /// </summary>
    /// <param name="existingCodes">The list of existing computer codes.</param>
    /// <returns>The next available computer code that is not present in the <paramref name="existingCodes"/>.</returns>
    /// <remarks>
    /// This method determines the next available computer code by finding the first alphabetical character that is not present in the <paramref name="existingCodes"/> list and returns it as the next free computer code.
    /// </remarks>
    [TestMethod]
    public void DetermineNextFreeComputerCode_Test()
    {
      var list = new[] {"A", "B"};

      var sut = new ComputerModel();

      sut.DetermineNextFreeComputerCode(list).ShouldEqual("C");
    }

    /// <summary>
    /// Determines the next free computer code based on the given list of existing codes.
    /// </summary>
    /// <param name="existingCodes">The list of existing computer codes.</param>
    /// <returns>The next available computer code that is not present in the <paramref name="existingCodes"/>.</returns>
    /// <remarks>
    /// This method determines the next free computer code by finding the first alphabetical character that is not present in the <paramref name="existingCodes"/>.
    /// If there are gaps in the alphabetical sequence, it fills the first gap. If there are no gaps, it skips to the next available character.
    /// </remarks>
    [TestMethod]
    public void DetermineNextFreeComputerCode2_Test()
    {
      var list = new List<string> {"B", "C", "F", "G"};

      var sut = new ComputerModel();

      sut.DetermineNextFreeComputerCode(list).ShouldEqual("A"); // fill hole

      list.AddRange(new[] { "A" });
      list.Sort();
      sut.DetermineNextFreeComputerCode(list).ShouldEqual("D"); // fill hole

      list.AddRange(new [] {"D", "E", "H"});
      list.Sort();
      sut.DetermineNextFreeComputerCode(list).ShouldEqual("J"); // skip I

      list.AddRange(new[]{"J","K"});
      sut.DetermineNextFreeComputerCode(list).ShouldEqual("M"); // skip L
    
      list.AddRange(new[]{"M","N"});
      sut.DetermineNextFreeComputerCode(list).ShouldEqual("P"); // skip O
    }

    /// <summary>
    /// Determines the next free computer code based on the given list of codes and returns the result.
    /// </summary>
    /// <param name="list">The list of computer codes to be considered.</param>
    /// <returns>The next available computer code based on the input <paramref name="list"/>.</returns>
    /// <remarks>
    /// This method determines the next available computer code by iterating through the given list of codes and finding the first available code based on alphabetical order.
    /// If no available code is found, it returns "AA" as the default code.
    /// </remarks>
    [TestMethod]
    public void DetermineNextFreeComputerCode3_Test()
    {
      string[] list = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray().Select(c=>c.ToString()).ToArray();

      var sut = new ComputerModel();

      sut.DetermineNextFreeComputerCode(list).ShouldEqual("AA");
    }

    /// <summary>
    /// Determines the next free computer code based on the input list of existing computer codes.
    /// </summary>
    /// <param name="list">The list of existing computer codes.</param>
    /// <returns>The next available computer code following the input list.</returns>
    /// <remarks>
    /// This method determines the next available computer code by finding the first unused code following the input list.
    /// If the input list contains "AA", the method should return "AB" as the next available code.
    /// </remarks>
    [TestMethod]
    public void DetermineNextFreeComputerCode4_Test()
    {
      var list = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray().Select(c=>c.ToString()).ToList();
      list.Add("AA");

      var sut = new ComputerModel();

      sut.DetermineNextFreeComputerCode(list).ShouldEqual("AB");
    }

    /// <summary>
    /// Determines the next free computer code based on the given list of existing codes.
    /// </summary>
    /// <param name="list">The list of existing computer codes.</param>
    /// <returns>The next available computer code following the existing codes in the list.</returns>
    /// <remarks>
    /// This method determines the next available computer code by finding the first alphabetical combination that is not present in the given list of existing codes.
    /// It starts from "AA" and iterates through the alphabetical combinations until it finds the first available code.
    /// If no code is available, it returns the next alphabetical combination after the last code in the list.
    /// </remarks>
    [TestMethod]
    public void DetermineNextFreeComputerCode5_Test()
    {
      var list = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray().Select(c=>c.ToString()).ToList();
      list.Add("AA");
      list.Add("AB"); 
      list.Add("AC");
      list.Add("AE");

      var sut = new ComputerModel();

      sut.DetermineNextFreeComputerCode(list).ShouldEqual("AD");
    }

    /// <summary>
    /// Determines the next free computer code from the given list of computer codes.
    /// </summary>
    /// <param name="list">The list of computer codes to determine the next free code from.</param>
    /// <returns>The next free computer code based on the input list.</returns>
    /// <remarks>
    /// This method iterates through the characters 'A' to 'C' and 'A' to 'Z' to generate all possible combinations of two characters and adds them to the input list.
    /// It then creates a new instance of the ComputerModel class and calls the DetermineNextFreeComputerCode method to determine the next free computer code from the modified list.
    /// The expected next free computer code is "DA".
    /// </remarks>
    [TestMethod]
    public void DetermineNextFreeComputerCode6_Test()
    {
      var list = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray().Select(c=>c.ToString()).ToList();
      
      for (var ch1 = 'A'; ch1 <= 'C'; ch1++)
      for (var ch2 = 'A'; ch2 <= 'Z'; ch2++)
      {
        list.Add("" + ch1 + ch2);
      }

      var sut = new ComputerModel();

      sut.DetermineNextFreeComputerCode(list).ShouldEqual("DA");
    }
  }
}