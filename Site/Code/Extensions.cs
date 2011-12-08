using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace TallyJ.Code
{
  public static class Extensions
  {

    /// <summary>
    ///   Not IsNullOrEmpty
    /// </summary>
    public static bool HasContent(this string input)
    {
      return !string.IsNullOrEmpty(input);
    }

    /// <summary>
    /// Return true if the input is empty or null.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool HasNoContent(this string input)
    {
      return string.IsNullOrEmpty(input);
    }

    /// <summary>
    /// Format for display in an MVC page.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static HtmlString AsRawHtml(this string input)
    {
      return new HtmlString(input);
    }


    /// <summary>
    ///   Split using a single separator
    /// </summary>
    public static string[] SplitWithString(this string input, string separator, StringSplitOptions stringSplitOptions)
    {
      return input == null
               ? null
               : input.Split(new[] {separator}, stringSplitOptions);
    }

    /// <summary>
    ///   Use the input string as the format with string.Format
    /// </summary>
    public static string FilledWithList<T>(this string input, IEnumerable<T> values)
    {
      if (values == null)
      {
        return input;
      }
      var array = values.ToArray();

      return string.Format(input, array);
    }

    /// <summary>
    /// Fill template with named items
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string FilledWithObject<T>(this string input, T value)
    {
      if (value == null)
      {
        return input;
      }
      var properties = value.GetAllProperties();

      return new TemplateHelper(input).FillByName(properties);
    }

    public static string FilledWithEachObject<T>(this string input, IEnumerable<T> values)
    {
      if (values == null)
      {
        return input;
      }
      // don't remove <T, string> or compile will fail
      var answers = values.Select<T, string>(input.FilledWithObject);
      return answers.JoinedAsString();
    }

    public static string FilledWithArray<T>(this string input, IEnumerable<T> values)
    {
      if (values == null)
      {
        return input;
      }

      return new TemplateHelper(input).FillByArray(values);
    }

    public static bool AsBool(this bool? input)
    {
      return input.HasValue && input.Value;
    }

    public static Guid AsGuid(this Guid? input)
    {
      return input.HasValue ? input.Value : Guid.Empty;
    }

    public static HtmlString AsHtmlString(this DateTime input, string format = "d MMMM yyyy")
    {
      if (input == DateTime.MinValue)
      {
        return "".AsRawHtml();
      }
      return input.ToString(format).AsRawHtml();
    }

    public static HtmlString AsHtmlString(this DateTime? input, string format = "d MMMM yyyy")
    {
      return input.HasValue ? input.Value.AsHtmlString(format) : "".AsRawHtml();
    }

    public static string AsString(this DateTime? input, string format = "d MMMM yyyy")
    {
      return input.HasValue ? input.Value.ToString(format) : "";
    }

    public static Guid? AsNullableGuid(this Guid input)
    {
      if (input == Guid.Empty) return null;

      return input;
    }

    public static IEnumerable<int> AsInts(this IEnumerable<string> input)
    {
      foreach (var s in input)
      {
        yield return s.AsInt();
      }
    }

    public static int AsInt(this object input)
    {
      return AsInt(input, 0);
    }

    public static int AsInt(this object input, int defaultValue)
    {
      if (input == null)
        return defaultValue;
      if (input == DBNull.Value)
        return defaultValue;

      try
      {
        return (int)Math.Truncate(Convert.ToDouble(input));
      }
      catch (Exception)
      {
        return defaultValue;
      }
      //return Util.Strings.Coalesce(input, 0);
    }


    /// <summary>
    ///   Use the input string as the format with string.Format
    /// </summary>
    public static string FilledWith(this string input, params object[] values)
    {
      if (input.HasNoContent())
        return string.Empty;

      if (values == null)
      {
        return input;
      }

        if(values.Length==1)
        {
            var value = values[0];
            if(value.GetType().Namespace!="System")
            {
                return input.FilledWithObject(value);
            }
        }

        return string.Format(input, values);
    }

    /// <summary>Return the URL to the content file, with a version number based on the timestamp.</summary>
    public static string AsClientFileWithVersion(this string contentFilePath, string productionNameModifier = "", string debuggingNameModifier = "")
    {
      bool UseDebugFiles = true; //TODO: move to config

      if (productionNameModifier.HasContent() || debuggingNameModifier.HasContent())
      {
        contentFilePath = contentFilePath.FilledWith(UseDebugFiles ? debuggingNameModifier : productionNameModifier);
      }

      var rawPath = HttpContext.Current.Request.MapPath(contentFilePath);
      var fileInfo = new FileInfo(rawPath);
      if (!fileInfo.Exists)
      {
        return String.Empty;
      }

      var version = fileInfo.LastWriteTime.Ticks.ToString();
      var trimmed = version.TrimEnd(new[] { '0' });
      const int sizeToUse = 5;

      var shortVersion = trimmed.Length <= sizeToUse ? trimmed : trimmed.Substring(trimmed.Length - sizeToUse, sizeToUse);
      return VirtualPathUtility.ToAbsolute(contentFilePath) + "?v=" + shortVersion;
    }

    /// <summary>
    ///   For an enumeration of strings, join them.
    /// </summary>
    public static string JoinedAsString(this IEnumerable<string> list)
    {
      return JoinedAsString(list, string.Empty);
    }

    /// <summary>
    ///   For an enumeration of strings, join them.
    /// </summary>
    public static string JoinedAsString(this IEnumerable<string> list, string separator)
    {
      return JoinedAsString(list, separator, false);
    }

    /// <summary>
    ///   For an enumeration of strings, join them.
    /// </summary>
    public static string JoinedAsString(this IEnumerable<string> list, string separator, bool skipBlanks)
    {
      return list.JoinedAsString(separator, string.Empty, string.Empty, skipBlanks);
    }

    /// <summary>
    ///   For an enumeration of strings, join them. Each item has itemLeft and itemRight added.
    /// </summary>
    public static string JoinedAsString(this IEnumerable<string> list, string separator, string itemLeft,
                      string itemRight, bool skipBlanks)
    {
      List<string> list2 = null;
      return list == null || (list2 = list.ToList()).Count() == 0
           ? string.Empty
           : string.Join(separator, list2.Where(s => !skipBlanks || s.HasContent()).Select(s => itemLeft + s + itemRight).ToArray());
    }

    public static string SurroundWith(this string input, string bothSides)
    {
      return SurroundWith(input, bothSides, bothSides);
    }

    public static string SurroundWith(this string input, string left, string right)
    {
      return left + input + right;
    }

    /// <summary>
    ///   Surround with left and right strings. If the input has no content, an empty string is returned.
    /// </summary>
    public static string SurroundContentWith(this string input, string left, string right)
    {
      if (input.HasNoContent())
        return string.Empty;

      return left + input + right;
    }


    /// <summary>Add new item to the end of the enumeration</summary>
    public static IEnumerable<T> AddTo<T>(this IEnumerable<T> input, List<T> addToThis)
    {
      var list = input.ToList();
      addToThis.AddRange(list);
      return list;
    }

    /// <summary>
    /// Get a named object from Session.
    /// </summary>
    /// <typeparam name="T">The type of the stored object</typeparam>
    /// <param name="input">Name in Session</param>
    /// <param name="defaultValue">Default value to use if nothing found</param>
    /// <returns></returns>
    public static T FromSession<T>(this string input, T defaultValue)
    {
      var value = HttpContext.Current.Session[input];
      if (value == null || value.GetType() != typeof(T))
      {
        return defaultValue;
      }
      return (T)value;
    }

    public static T SetInSession<T>(this string input, T newValue)
    {
      HttpContext.Current.Session[input] = newValue;
      return newValue;
    }


    /// <summary>
    ///   If input is empty, use <paramref name = "defaultValue" />
    /// </summary>
    public static string DefaultTo(this string input, string defaultValue)
    {
      return input.HasNoContent() ? defaultValue : input;
    }

    /// <summary>
    ///   If input is 0, use <paramref name = "defaultValue" />
    /// </summary>
    public static int DefaultTo(this int input, int defaultValue)
    {
      return input == 0 ? defaultValue : input;
    }


    public static string QuotedForJavascript(this string input)
    {
      return String.Format("\"{0}\"", input
                        .CleanedForJavascriptStrings()
                        .Replace("\"", "\\\"")
        );
    }

    public static string CleanedForJavascriptStrings(this string input)
    {
      if (input.HasNoContent())
        return string.Empty;
      return input
        .Replace(@"\", @"\\")
        .Replace("\n", "\\n")
        .Replace("\r", string.Empty);
    }


    /// <summary>
    /// Converts this object to a JSON string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string SerializedAsJsonString(this object input)
    {
      return new JavaScriptSerializer().Serialize(input);
    }


    /// <summary>
    /// Wrap object for returning to client as a JsonResult
    /// </summary>
    /// <param name="input"></param>
    /// <param name="behavior">Behavior to use, usually only POST is allowed.</param>
    /// <returns></returns>
    public static JsonResult AsJsonResult(this object input, JsonRequestBehavior behavior = JsonRequestBehavior.DenyGet)
    {
      var jsonResult = new JsonResult
      {
        ContentType = "text/plain", // allow client full control over reading response (don't send as JSON type)
        Data = input,
        JsonRequestBehavior = behavior
      };
      return jsonResult;
    }

    public static TimeSpan seconds(this int input)
    {
      return new TimeSpan(0, 0, input);
    }
    public static TimeSpan minutes(this int input)
    {
      return new TimeSpan(0, input, 0);
    }

  }
}