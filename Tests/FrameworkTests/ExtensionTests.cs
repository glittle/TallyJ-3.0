using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code;
using Tests.Support;

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

    [TestMethod]
    public void FilledWith_List1()
    {
      var values = new object[] {"string", 1234};
      var template = "0:{0} 1:{1}";

      template.FilledWith(values).ShouldEqual("0:string 1:1234");
    }

    [TestMethod]
    public void FilledWith_List2()
    {
      var values = new object[] {"string", 1234};
      var template = "0:{0} 0:{0}";

      template.FilledWith(values).ShouldEqual("0:string 0:string");
    }

    [TestMethod]
    [ExpectedException(typeof (FormatException))]
    public void FilledWithArray_List_Fail1()
    {
      var values = new object[] {"string", 1234};
      var template = "0:{0} 1:{1} 2:{2}"; // too many items in template

      template.FilledWithList(values).ShouldEqual(" fails - will through exception ");
    }

    [TestMethod]
    public void FilledWithArray_List3()
    {
      bool[] values = {false, true};
      var template = "0:{0} 1:{1}";

      template.FilledWithArray(values).ShouldEqual("0:False 1:True");
    }

    [TestMethod]
    public void FilledWithObject()
    {
      var template = "A:{A} B:{B}";
      var item = new
                   {
                     A = 23,
                     B = "Hello"
                   };

      template.FilledWithObject(item).ShouldEqual("A:23 B:Hello");
    }

    [TestMethod]
    public void FilledWithObject2()
    {
      var template = "Name:{Name} Recursive:{MyName}";
      var item = new
                   {
                     Name = "John",
                     MyName = "{Name}"
                   };

      template.FilledWithObject(item).ShouldEqual("Name:John Recursive:John");
    }
  }
}