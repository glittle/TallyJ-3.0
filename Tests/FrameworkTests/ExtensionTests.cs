using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code;
using Tests.Support;

namespace Tests.FrameworkTests
{
  [TestClass]
  public class ExtensionTests
  {
    [TestMethod]
    public void HasContent_Test()
    {
      "".HasContent().ShouldEqual(false);
      " ".HasContent().ShouldEqual(true);
      "Hello".HasContent().ShouldEqual(true);

      string s = null;
      s.HasContent().ShouldEqual(false);

    }

    [TestMethod]
    public void HasNoContent_Test()
    {
      "".HasNoContent().ShouldEqual(true);
      " ".HasNoContent().ShouldEqual(false);
      "Hello".HasNoContent().ShouldEqual(false);

      string s = null;
      s.HasNoContent().ShouldEqual(true);
    }

    [TestMethod]
    public void AsRawHtml_Test()
    {
      // "abc".AsRawHtml().ShouldEqual(new HtmlString("abc"));
    }

    [TestMethod]
    public void SplitWithString_Test()
    {
      var r1 = "abc;def;ghi".SplitWithString(",");
      r1.Length.ShouldEqual(1);
      r1[0].ShouldEqual("abc;def;ghi");

      var r2 = "abc;def;ghi".SplitWithString(";");
      r2.Length.ShouldEqual(3);
      r2[0].ShouldEqual("abc");
      r2[1].ShouldEqual("def");
      r2[2].ShouldEqual("ghi");

      var r3 = " abc ; def ;;;; ghi".SplitWithString(";");
      r3.Length.ShouldEqual(3);
      r3[0].ShouldEqual(" abc ");
      r3[1].ShouldEqual(" def ");
      r3[2].ShouldEqual(" ghi");

      string s = null;
      s.SplitWithString("x").ShouldEqual(null);
    }


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
      var values = new object[] { "string", 1234 };
      var template = "0:{0} 1:{1}";

      template.FilledWith(values).ShouldEqual("0:string 1:1234");
    }

    [TestMethod]
    public void FilledWith_List2()
    {
      var values = new object[] { "string", 1234 };
      var template = "0:{0} 0:{0}";

      template.FilledWith(values).ShouldEqual("0:string 0:string");
    }

    [TestMethod]
    [ExpectedException(typeof(FormatException))]
    public void FilledWithArray_List_Fail1()
    {
      var values = new object[] { "string", 1234 };
      var template = "0:{0} 1:{1} 2:{2}"; // too many items in template

      template.FilledWithList(values).ShouldEqual(" fails - will through exception ");
    }

    [TestMethod]
    public void FilledWithArray_List3()
    {
      bool[] values = { false, true };
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

    [TestMethod]
    public void FilledWithEachObject_Test()
    {
      var objects = new[]
                      {
                        new { A = "abc", B = "def" },
                        new { A = "aaa", B = "ddd" },
                      };

      var template = "Item: {A}{B};";
      template.FilledWithEachObject(objects).ShouldEqual("Item: abcdef;Item: aaaddd;");
    }



    [TestMethod]
    public void TestGetAllMsg()
    {
      new Exception("Test 123").GetAllMsgs(",").ShouldEqual("Test 123");

      new Exception("Test 123", new ExternalException("Test 456"))
        .GetAllMsgs(",").ShouldEqual("Test 123,Test 456");
    }

    [TestMethod]
    public void AsBool_Test()
    {
      bool? item = null;
      item.AsBoolean().ShouldEqual(false);

      item = true;
      item.AsBoolean().ShouldEqual(true);

      item = false;
      item.AsBoolean().ShouldEqual(false);
    }

    [TestMethod]
    public void AsNullableTrueOrNull_Test()
    {
      bool? item = null;
      bool? trueValue = true;

      item.OnlyIfTrue().ShouldEqual(null);


      item = false;
      var item2 = false;
      item.OnlyIfTrue().ShouldEqual(null);
      item2.OnlyIfTrue().ShouldEqual(null);

      item = true;
      item2 = true;
      item.OnlyIfTrue().ShouldEqual(trueValue);
      item2.OnlyIfTrue().ShouldEqual(trueValue);
    }

    [TestMethod]
    public void AsNullableFalseOrNull_Test()
    {
      bool? item = null;
      bool? falseValue = false;

      item.OnlyIfFalse().ShouldEqual(falseValue);

      item = false;
      var item2 = false;
      item.OnlyIfFalse().ShouldEqual(falseValue);
      item2.OnlyIfFalse().ShouldEqual(falseValue);

      item = true;
      item2 = true;
      item.OnlyIfFalse().ShouldEqual(null);
      item2.OnlyIfFalse().ShouldEqual(null);

    }


    [TestMethod]
    public void AsGuid_Test()
    {
      var newGuid = Guid.NewGuid();

      Guid? item = newGuid;
      item.AsGuid().ShouldEqual(newGuid);

      item = null;
      item.AsGuid().ShouldEqual(Guid.Empty);
    }

    [TestMethod]
    public void AsBool_String_Test()
    {
      "true".AsBoolean().ShouldEqual(true);
      "True".AsBoolean().ShouldEqual(true);

      "false".AsBoolean().ShouldEqual(false);
      "False".AsBoolean().ShouldEqual(false);

      "1".AsBoolean().ShouldEqual(true);
      "0".AsBoolean().ShouldEqual(false);

      "".AsBoolean().ShouldEqual(false);
      "hello".AsBoolean().ShouldEqual(false);

      string nullstring = null;
      nullstring.AsBoolean().ShouldEqual(false);
    }

    [TestMethod]
    public void Accents_Test()
    {
      "Bahá'í".WithoutDiacritics().ShouldEqual("Baha'i");
      "Bahá'í".WithoutDiacritics(true).ShouldEqual("baha'i");
      "Üzbek, tienne".WithoutDiacritics().ShouldEqual("Uzbek, tienne");
      "Üzbek, tienne".WithoutDiacritics(true).ShouldEqual("uzbek, tienne");
    }

    [TestMethod]
    public void ReplacePunctuation_Test()
    {
      const char sep = '$';

      "".ReplacePunctuation(sep).ShouldEqual("");
      "a b".ReplacePunctuation(sep).ShouldEqual("a$b");
      "ab".ReplacePunctuation(sep).ShouldEqual("ab");
      "a-b!".ReplacePunctuation(sep).ShouldEqual("a$b$");
      "o'conner".ReplacePunctuation(sep).ShouldEqual("o$conner");
      "ab==123".ReplacePunctuation(sep).ShouldEqual("ab$$123");
    }

    [TestMethod]
    public void AsPctString_Test()
    {


      50.PercentOf(100).ShouldEqual("50%");
      1.PercentOf(100).ShouldEqual("1%");
      100.PercentOf(10).ShouldEqual("1000%");
      45.PercentOf(100).ShouldEqual("45%");

      45.PercentOf(100, surroundWithParen: true).ShouldEqual("(45%)");

      1.PercentOf(0).ShouldEqual("-");
      50.PercentOf(0).ShouldEqual("-");
      0.PercentOf(1).ShouldEqual("0%");
      0.PercentOf(50).ShouldEqual("0%");

      0.PercentOf(50, 3).ShouldEqual("0.000%");
      0.PercentOf(50, -3).ShouldEqual("0%");
      
      0.PercentOf(50, showZero:false).ShouldEqual("-");

      1.PercentOf(10000).ShouldEqual("0%");
      1.PercentOf(10000, 1).ShouldEqual("0.0%");
      1.PercentOf(10000, -1).ShouldEqual("0%");

      44.PercentOf(1000).ShouldEqual("4%");
      45.PercentOf(1000).ShouldEqual("4%"); // round down
      46.PercentOf(1000).ShouldEqual("5%");
      
      74.PercentOf(1000).ShouldEqual("7%");
      75.PercentOf(1000).ShouldEqual("8%"); //round up
      76.PercentOf(1000).ShouldEqual("8%");

      46.PercentOf(1000, 1).ShouldEqual("4.6%");
      46.PercentOf(1000, 2).ShouldEqual("4.60%");

      46.PercentOf(1000, -2).ShouldEqual("4.6%");
    }
  }

}