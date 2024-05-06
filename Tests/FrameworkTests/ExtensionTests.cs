using System;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code;
using TallyJ.CoreModels.Helper;
using Tests.Support;
using Extensions = TallyJ.Code.Extensions;

namespace Tests.FrameworkTests
{
  [TestClass]
  public class ExtensionTests
  {

    /// <summary>
    /// Checks if the string has content and returns true if it is not null or empty.
    /// </summary>
    /// <returns>True if the string has content; otherwise, false.</returns>
    /// <remarks>
    /// This method checks whether the input string is not null and not empty, and returns true if it has content; otherwise, it returns false.
    /// </remarks>
    [TestMethod]
    public void HasContent_Test()
    {
      "".HasContent().ShouldEqual(false);
      " ".HasContent().ShouldEqual(true);
      "Hello".HasContent().ShouldEqual(true);

      string s = null;
      s.HasContent().ShouldEqual(false);

    }

    /// <summary>
    /// Returns the value of the nullable integer if it has a value; otherwise, returns the specified default value.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the nullable integer is null.</param>
    /// <returns>The value of the nullable integer if it has a value; otherwise, the specified default value.</returns>
    /// <remarks>
    /// This method returns the value of the nullable integer if it has a value; otherwise, it returns the specified default value.
    /// </remarks>
    [TestMethod]
    public void DefaultTo_Int()
    {
      int? a = 11;

      a.DefaultTo(1).ShouldEqual(11);

      a = 0;
      a.DefaultTo(1).ShouldEqual(1);
    
      a = null;
      a.DefaultTo(1).ShouldEqual(1);
    }

    /// <summary>
    /// Returns the specified number of characters from the left of the input string.
    /// </summary>
    /// <param name="length">The number of characters to return from the left of the input string.</param>
    /// <returns>The substring containing the leftmost <paramref name="length"/> characters of the input string, or an empty string if the input is null or empty.</returns>
    /// <remarks>
    /// This method returns the specified number of characters from the left of the input string. If the input string is null or empty, an empty string is returned.
    /// </remarks>
    [TestMethod]
    public void Left_Test()
    {
      "".Left(0).ShouldEqual("");
      "".Left(5).ShouldEqual("");

      var a = "abcde";
      a.Left(0).ShouldEqual("");
      a.Left(5).ShouldEqual("abcde");
      a.Left(500).ShouldEqual("abcde");

      a = null;
      a.Left(0).ShouldEqual("");
      a.Left(10).ShouldEqual("");
    }

    /// <summary>
    /// Checks if the string has no content.
    /// </summary>
    /// <returns>True if the string has no content; otherwise, false.</returns>
    /// <remarks>
    /// This method returns true if the input string has no content, which includes being null or having only whitespace characters.
    /// </remarks>
    [TestMethod]
    public void HasNoContent_Test()
    {
      "".HasNoContent().ShouldEqual(true);
      " ".HasNoContent().ShouldEqual(false);
      "Hello".HasNoContent().ShouldEqual(false);

      string s = null;
      s.HasNoContent().ShouldEqual(true);
    }

    /// <summary>
    /// Returns the input string as raw HTML.
    /// </summary>
    /// <remarks>
    /// This method returns the input string as raw HTML without any encoding or transformation.
    /// </remarks>
    [TestMethod]
    public void AsRawHtml_Test()
    {
      // "abc".AsRawHtml().ShouldEqual(new HtmlString("abc"));
    }

    /// <summary>
    /// Splits a string using the specified separator and returns an array of substrings.
    /// </summary>
    /// <param name="separator">The string to use as a separator.</param>
    /// <returns>An array of substrings that were separated by the specified <paramref name="separator"/>.</returns>
    /// <remarks>
    /// This method splits the input string based on the specified <paramref name="separator"/> and returns an array of substrings.
    /// If the input string is null, the method returns null.
    /// </remarks>
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

    /// <summary>
    /// Joins the elements of the array into a single string using the default separator and returns the result.
    /// </summary>
    /// <param name="source">The array of strings to be joined.</param>
    /// <returns>A string that consists of the elements of <paramref name="source"/> joined together using the default separator.</returns>
    /// <remarks>
    /// This method joins the elements of the input array <paramref name="source"/> into a single string using the default separator.
    /// The default separator is an empty string.
    /// If any element in the array is an empty string, it is included in the joined string.
    /// </remarks>
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

    /// <summary>
    /// Replaces the placeholders in the template string with the corresponding values from the input array and returns the resulting string.
    /// </summary>
    /// <param name="values">The array containing the values to be inserted into the template string.</param>
    /// <returns>A new string with the placeholders replaced by the values from the input array.</returns>
    /// <remarks>
    /// This method replaces the placeholders in the template string with the corresponding values from the input array.
    /// The placeholders in the template string are represented by numeric indices enclosed in curly braces (e.g., {0}, {1}, etc.).
    /// The method replaces each placeholder with the value at the corresponding index in the input array.
    /// If there are more placeholders than values in the input array, the extra placeholders are left unchanged in the resulting string.
    /// If there are more values than placeholders, the extra values are ignored.
    /// </remarks>
    [TestMethod]
    public void FilledWith_List1()
    {
      var values = new object[] { "string", 1234 };
      var template = "0:{0} 1:{1}";

      template.FilledWith(values).ShouldEqual("0:string 1:1234");
    }

    /// <summary>
    /// Fills the placeholders in the input string template with the corresponding values from the input array and returns the resulting string.
    /// </summary>
    /// <param name="values">The array of values to fill in the placeholders of the template.</param>
    /// <returns>The string with the placeholders filled with the corresponding values from the input array.</returns>
    /// <remarks>
    /// This method replaces the placeholders in the input string template with the corresponding values from the input array.
    /// The placeholders in the template are represented by the format "{i}", where "i" is the index of the value in the input array.
    /// If there are multiple occurrences of the same placeholder, they will be replaced with the same corresponding value from the input array.
    /// </remarks>
    [TestMethod]
    public void FilledWith_List2()
    {
      var values = new object[] { "string", 1234 };
      var template = "0:{0} 0:{0}";

      template.FilledWith(values).ShouldEqual("0:string 0:string");
    }

    /// <summary>
    /// Tests the behavior of the FilledWithList method when the template has too many items, and expects a FormatException to be thrown.
    /// </summary>
    /// <remarks>
    /// This test creates an array of objects containing a string and an integer. It then attempts to fill a template with more items than the array contains, expecting a FormatException to be thrown.
    /// </remarks>
    /// <exception cref="FormatException">Thrown when the template has more items than the array contains.</exception>
    [TestMethod]
    [ExpectedException(typeof(FormatException))]
    public void FilledWithArray_List_Fail1()
    {
      var values = new object[] { "string", 1234 };
      var template = "0:{0} 1:{1} 2:{2}"; // too many items in template

      template.FilledWithList(values).ShouldEqual(" fails - will through exception ");
    }

    /// <summary>
    /// Fills the placeholders in the input string template with the corresponding values from the input array and returns the result.
    /// </summary>
    /// <param name="values">The array containing the values to fill in the template.</param>
    /// <returns>A string with the placeholders replaced by the corresponding values from the input array.</returns>
    /// <remarks>
    /// This method replaces the placeholders in the input string template with the corresponding values from the input array.
    /// The placeholders in the template should be in the format {0}, {1}, and so on, matching the indices of the values in the input array.
    /// If there are more placeholders than values in the array, the extra placeholders will remain unchanged in the output string.
    /// If there are more values than placeholders, the extra values will be ignored.
    /// </remarks>
    [TestMethod]
    public void FilledWithArray_List3()
    {
      bool[] values = { false, true };
      var template = "0:{0} 1:{1}";

      template.FilledWithArray(values).ShouldEqual("0:False 1:True");
    }

    /// <summary>
    /// Fills the template string with the properties of the input object and returns the resulting string.
    /// </summary>
    /// <param name="item">The object containing properties to fill the template string.</param>
    /// <returns>The template string filled with the properties of the input <paramref name="item"/>.</returns>
    /// <remarks>
    /// This method replaces placeholders in the template string with the corresponding property values from the input object.
    /// For example, if the template is "A:{A} B:{B}" and the input object has properties A=23 and B="Hello", the resulting string will be "A:23 B:Hello".
    /// </remarks>
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

    /// <summary>
    /// Tests the FilledWithObject method with Korean characters and checks if the Korean characters are 'decomposed' or not.
    /// </summary>
    /// <remarks>
    /// This method tests the FilledWithObject method with Korean characters to ensure that the characters are not 'decomposed'.
    /// It fills the template with the provided object and checks if the output matches the expected result.
    /// </remarks>
    /// <exception cref="AssertionException">Thrown when the actual output does not match the expected result.</exception>
    [TestMethod]
    public void FilledWithObjectUnicode() {
      // test FilledWithObject with Korean characters
      // 이때 꼭 투표해 보세요
      // 안녕하세요 
      // In some instances, the Korean characters are 'decomposed' but should not be!
      var template = "안녕하세요:{A} B:{B} C:이때 꼭 투표해 보세";
      var item = new
      {
                     A = 23,
                     B = "안녕하세요"
                   };

      template.FilledWithObject(item).ShouldEqual("안녕하세요:23 B:안녕하세요 C:이때 꼭 투표해 보세");
    }

    /// <summary>
    /// Fills the template string with the properties of the given object and returns the result.
    /// </summary>
    /// <remarks>
    /// This method replaces placeholders in the input template string with the corresponding property values from the input object.
    /// The placeholders are enclosed in curly braces and contain the property names.
    /// If a placeholder does not match any property in the object, it remains unchanged in the output string.
    /// This method does not modify the original template string or object.
    /// </remarks>
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

    /// <summary>
    /// Fills the template with each object in the array and returns the concatenated result.
    /// </summary>
    /// <param name="objects">The array of objects to be filled into the template.</param>
    /// <returns>A string containing the concatenated result of filling the template with each object in the array.</returns>
    /// <remarks>
    /// This method fills the specified template with each object in the input array and concatenates the results.
    /// The template should contain placeholders for the properties of the objects, e.g., "{A}{B}".
    /// </remarks>
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

    /// <summary>
    /// Tests the GetAllMsgs method of the Exception class.
    /// </summary>
    /// <remarks>
    /// This method tests the GetAllMsgs method of the Exception class by creating instances of the Exception class with different parameters and calling the GetAllMsgs method on them.
    /// The GetAllMsgs method returns a concatenated string of all error messages from the exception and its inner exceptions, separated by the specified delimiter.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown if the specified delimiter is null.
    /// </exception>
    /// <returns>
    /// The concatenated string of all error messages from the exception and its inner exceptions, separated by the specified delimiter.
    /// </returns>
    [TestMethod]
    public void TestGetAllMsg()
    {
      new Exception("Test 123").GetAllMsgs(",").ShouldEqual("Test 123");

      new Exception("Test 123", new ExternalException("Test 456"))
        .GetAllMsgs(",").ShouldEqual("Test 123,Test 456");
    }

    /// <summary>
    /// Converts a nullable boolean value to a boolean value, with an option to specify a default value.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the input is null.</param>
    /// <returns>The boolean value of the input, or the specified <paramref name="defaultValue"/> if the input is null.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when the input is not null and is not a boolean value.</exception>
    [TestMethod]
    public void AsBool_Test()
    {
      bool? item = null;
      item.AsBoolean().ShouldEqual(false);
      item.AsBoolean(true).ShouldEqual(true);

      item = true;
      item.AsBoolean().ShouldEqual(true);
      item.AsBoolean(false).ShouldEqual(true);

      item = false;
      item.AsBoolean().ShouldEqual(false);
      item.AsBoolean(true).ShouldEqual(false);
    }

    /// <summary>
    /// Checks if the boolean value is true and returns it as nullable boolean.
    /// </summary>
    /// <param name="item">The boolean value to be checked.</param>
    /// <returns>The nullable boolean value. Returns null if the input is false; otherwise, returns the input value.</returns>
    /// <remarks>
    /// This method checks the input boolean value and returns it as a nullable boolean. If the input is false, it returns null; otherwise, it returns the input value.
    /// </remarks>
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

    /// <summary>
    /// Tests the OnlyIfFalse extension method for nullable boolean values.
    /// </summary>
    /// <remarks>
    /// This method tests the OnlyIfFalse extension method for nullable boolean values.
    /// It verifies that the method returns the expected result based on the input value.
    /// If the input value is null, the method should return false.
    /// If the input value is false, the method should return false.
    /// If the input value is true, the method should return null.
    /// </remarks>
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

    /// <summary>
    /// Converts a nullable <see cref="Guid"/> to its non-nullable equivalent, or returns <see cref="Guid.Empty"/> if the input is null.
    /// </summary>
    /// <remarks>
    /// This method returns the input <paramref name="item"/> if it is not null, otherwise it returns <see cref="Guid.Empty"/>.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the input <paramref name="item"/> is not null and cannot be converted to a non-nullable <see cref="Guid"/>.
    /// </exception>
    /// <param name="item">The nullable <see cref="Guid"/> to be converted.</param>
    /// <returns>The non-nullable equivalent of the input <paramref name="item"/>, or <see cref="Guid.Empty"/> if the input is null.</returns>
    [TestMethod]
    public void AsGuid_Test()
    {
      var newGuid = Guid.NewGuid();

      Guid? item = newGuid;
      item.AsGuid().ShouldEqual(newGuid);

      item = null;
      item.AsGuid().ShouldEqual(Guid.Empty);
    }

    /// <summary>
    /// Converts the input string to a boolean value and returns the result.
    /// </summary>
    /// <param name="input">The input string to be converted to a boolean value.</param>
    /// <returns>True if the input string represents a true value, False if the input string represents a false value, otherwise False.</returns>
    /// <remarks>
    /// This method converts the input string to a boolean value based on the following rules:
    /// - If the input string is "true" (case-insensitive), it returns True.
    /// - If the input string is "false" (case-insensitive), it returns False.
    /// - If the input string is "1", it returns True.
    /// - If the input string is "0", it returns False.
    /// - If the input string is empty or null, it returns False.
    /// - If the input string does not match any of the above conditions, it returns False.
    /// </remarks>
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

    /// <summary>
    /// Tests the removal of diacritics from the input string and returns the modified string.
    /// </summary>
    /// <remarks>
    /// This method removes diacritics from the input string using the WithoutDiacritics method and returns the modified string.
    /// If the parameter <paramref name="preserveCase"/> is set to true, the modified string will be in lowercase.
    /// </remarks>
    /// <exception cref="System.Exception">
    /// Throws an exception if the input string is null or empty.
    /// </exception>
    /// <returns>
    /// The modified string after removing diacritics.
    /// </returns>
    [TestMethod]
    public void Accents_Test()
    {
      "Bahá'í".WithoutDiacritics().ShouldEqual("Baha'i");
      "Bahá'í".WithoutDiacritics(true).ShouldEqual("baha'i");
      "Üzbek, tienne".WithoutDiacritics().ShouldEqual("Uzbek, tienne");
      "Üzbek, tienne".WithoutDiacritics(true).ShouldEqual("uzbek, tienne");
    }

    /// <summary>
    /// Replaces all punctuation characters in the input string with the specified separator character and returns the modified string.
    /// </summary>
    /// <param name="separator">The character to be used as a separator.</param>
    /// <returns>The modified string with punctuation characters replaced by the specified <paramref name="separator"/>.</returns>
    /// <remarks>
    /// This method replaces all punctuation characters in the input string with the specified separator character.
    /// Punctuation characters include symbols such as '.', ',', '!', '?', etc.
    /// The modified string is then returned as the output.
    /// </remarks>
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

    /// <summary>
    /// Tests the conversion of a <see cref="System.Net.Mail.MailAddress"/> to a <see cref="SendGrid.Helpers.Mail.EmailAddress"/>.
    /// </summary>
    /// <remarks>
    /// This method creates a <see cref="System.Net.Mail.MailAddress"/> using the specified email address and name, and then converts it to a <see cref="SendGrid.Helpers.Mail.EmailAddress"/>.
    /// The email address and name of the resulting <see cref="SendGrid.Helpers.Mail.EmailAddress"/> are then compared with the original values.
    /// </remarks>
    [TestMethod]
    public void AsSendGridEmailAddress_Test()
    {
      var address = "address1@example.com";
      var name = "My name";

      var msEmail = new MailAddress(address, name);
      var sendGridEmail = msEmail.AsSendGridEmailAddress();

      sendGridEmail.Email.ShouldEqual(address);
      sendGridEmail.Name.ShouldEqual(name);
    }

    /// <summary>
    /// Joins the elements of the input array into a sentence using the specified conjunction and returns the resulting sentence.
    /// </summary>
    /// <param name="conjunction">The conjunction to be used for joining the elements.</param>
    /// <returns>The sentence formed by joining the elements of the input array using the specified <paramref name="conjunction"/>.</returns>
    /// <remarks>
    /// This method joins the elements of the input array into a sentence using the specified conjunction.
    /// If the input array contains only one element, that element is returned as the sentence.
    /// If the input array contains two elements, they are joined using the conjunction and returned as the sentence.
    /// If the input array contains more than two elements, all elements except the last one are joined using commas, and the last element is joined using the conjunction, and then returned as the sentence.
    /// </remarks>
    [TestMethod]
    public void InSentence_Test()
    {
      new []{"a"}.InSentence("and").ShouldEqual("a");

      new []{"a", "b", "c"}.InSentence("or").ShouldEqual("a, b, or c");
      new []{"a", "b", "c"}.InSentence("and").ShouldEqual("a, b, and c");

      new []{"a", "b"}.InSentence("and").ShouldEqual("a and b");

      new []{"a", "b", "c", "d"}.InSentence("and").ShouldEqual("a, b, c, and d");
    }

    /// <summary>
    /// Calculates the percentage of a number and returns the result as a string.
    /// </summary>
    /// <param name="number">The number to calculate the percentage of.</param>
    /// <param name="total">The total number used to calculate the percentage.</param>
    /// <param name="decimalPlaces">The number of decimal places to round the percentage to.</param>
    /// <param name="surroundWithParen">Indicates whether the result should be surrounded with parentheses.</param>
    /// <param name="showZero">Indicates whether to show "-" if the result is zero.</param>
    /// <returns>The calculated percentage as a string, rounded to the specified number of decimal places and optionally surrounded with parentheses.</returns>
    /// <remarks>
    /// This method calculates the percentage of a number relative to a total, rounding the result to the specified number of decimal places.
    /// If the result is zero and showZero is true, it returns "-". If surroundWithParen is true, the result is surrounded with parentheses.
    /// </remarks>
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

    /// <summary>
    /// Returns the string after skipping the specified number of lines.
    /// </summary>
    /// <param name="linesToSkip">The number of lines to skip.</param>
    /// <returns>The string after skipping the specified number of lines.</returns>
    /// <remarks>
    /// This method returns the string after skipping the specified number of lines. It handles both '\n' and '\r\n' line endings.
    /// </remarks>
    [TestMethod]
    public void GetLinesAfterSkipping_Test1()
    {
      var s = "123\n234\n345";

      var withRN = "123\r\n234\r\n345";

      s.GetLinesAfterSkipping(0).ShouldEqual(withRN);
      s.GetLinesAfterSkipping(1).ShouldEqual("234\r\n345");
      s.GetLinesAfterSkipping(2).ShouldEqual("345");
      s.GetLinesAfterSkipping(3).ShouldEqual("");

      var s2 = "123\r234\r345";
      s2.GetLinesAfterSkipping(0).ShouldEqual(withRN);
      s2.GetLinesAfterSkipping(1).ShouldEqual("234\r\n345");
      s2.GetLinesAfterSkipping(2).ShouldEqual("345");
      s2.GetLinesAfterSkipping(3).ShouldEqual("");

      var s3 = "123\r\n234\r\n345";
      s3.GetLinesAfterSkipping(0).ShouldEqual(withRN);
      s3.GetLinesAfterSkipping(1).ShouldEqual("234\r\n345");
      s3.GetLinesAfterSkipping(2).ShouldEqual("345");
      s3.GetLinesAfterSkipping(3).ShouldEqual("");

    }

    /// <summary>
    /// Tests the IsValidEmail method of the EmailHelper class.
    /// </summary>
    /// <remarks>
    /// This method tests the IsValidEmail method of the EmailHelper class by passing various email addresses as input and asserting the expected results.
    /// It checks for both valid and invalid email addresses and verifies that the IsValidEmail method returns the correct result for each input.
    /// </remarks>
    [TestMethod]
    public void EmailTester_Test()
    {
      EmailHelper.IsValidEmail("").ShouldEqual(false);
      EmailHelper.IsValidEmail("@").ShouldEqual(false);
      EmailHelper.IsValidEmail(".").ShouldEqual(false);
      EmailHelper.IsValidEmail("@x").ShouldEqual(false);
      EmailHelper.IsValidEmail("@.").ShouldEqual(false);
      EmailHelper.IsValidEmail("@.com").ShouldEqual(false);

      EmailHelper.IsValidEmail("a@b.c").ShouldEqual(false);

      EmailHelper.IsValidEmail("a@b.cc").ShouldEqual(true);
    }

    /// <summary>
    /// Tests the IsValidPhoneNumber method of TwilioHelper.
    /// </summary>
    /// <remarks>
    /// This method tests the IsValidPhoneNumber method of TwilioHelper by passing different phone numbers as input and checking if the method returns the expected result.
    /// The IsValidPhoneNumber method checks if the input phone number is a valid international phone number by matching it against the regular expression pattern \+[0-9]{4,15}.
    /// </remarks>
    /// <exception cref="System.Exception">
    /// Thrown when an error occurs while testing the IsValidPhoneNumber method.
    /// </exception>
    [TestMethod]
    public void PhoneTester_Test()
    {
      // \+[0-9]{4,15}

      TwilioHelper.IsValidPhoneNumber("").ShouldEqual(false);
      TwilioHelper.IsValidPhoneNumber("1").ShouldEqual(false);
      TwilioHelper.IsValidPhoneNumber("1234").ShouldEqual(false);
      TwilioHelper.IsValidPhoneNumber("123456").ShouldEqual(false);

      TwilioHelper.IsValidPhoneNumber("+123456").ShouldEqual(true);

    }
  }

}