using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Models;
using System.Linq;

namespace Tests.BusinessTests
{
  [TestClass]
  public class ComputerTests
  {
    [TestMethod]
    public void DetermineNextFreeComputerCode_Test()
    {
      var list = new[] {"A", "B"};

      var sut = new ComputerModel();

      sut.DetermineNextFreeComputerCode(list).ShouldEqual("C");
    }
    [TestMethod]
    public void DetermineNextFreeComputerCode2_Test()
    {
      var list = new[] {"A", "B", "C", "F", "G"};

      var sut = new ComputerModel();

      sut.DetermineNextFreeComputerCode(list).ShouldEqual("D");
    }

    [TestMethod]
    public void DetermineNextFreeComputerCode3_Test()
    {
      string[] list = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray().Select(c=>c.ToString()).ToArray();

      var sut = new ComputerModel();

      sut.DetermineNextFreeComputerCode(list).ShouldEqual("AA");
    }

    [TestMethod]
    public void DetermineNextFreeComputerCode4_Test()
    {
      var list = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray().Select(c=>c.ToString()).ToList();
      list.Add("AA");

      var sut = new ComputerModel();

      sut.DetermineNextFreeComputerCode(list).ShouldEqual("AB");
    }

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