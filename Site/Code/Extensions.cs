using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using RazorEngine.Text;
using SendGrid.Helpers.Mail;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.VoterAccountModels;
using TallyJ.EF;

namespace TallyJ.Code
{
  public static class Extensions
  {
    private static readonly JsonSerializerSettings _jsonSerializerSettings;

    static Extensions()
    {
      _jsonSerializerSettings = new JsonSerializerSettings
      {
        ReferenceLoopHandling = ReferenceLoopHandling.Error,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
      };
    }

    /// <summary>
    ///   Not IsNullOrEmpty
    /// </summary>
    [DebuggerStepThrough]
    public static bool HasContent(this string input)
    {
      return !string.IsNullOrEmpty(input);
    }

    /// <Summary>Whether this guid is the Empty guid or not</Summary>
    [DebuggerStepThrough]
    public static bool HasContent(this Guid input)
    {
      return input != Guid.Empty;
    }

    /// <summary>
    ///   Return true if the input is empty or null.
    /// </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
    [DebuggerStepThrough]
    public static bool HasNoContent(this string input)
    {
      return string.IsNullOrEmpty(input);
    }

    [DebuggerStepThrough]
    public static bool HasNoContent(this int? input)
    {
      return !input.HasValue || input.Value == 0;
    }

    /// <summary>
    ///   Format for display in an MVC page.
    /// </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
   // [DebuggerStepThrough]
    public static HtmlString AsRawHtml(this string input)
    {
      return new HtmlString(input);
    }

    /// <summary>
    ///   Format for display in an MVC page.
    /// </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
    public static MvcHtmlString AsRawMvcHtml(this string input)
    {
      return new MvcHtmlString(input);
    }

    /// <summary>
    /// A way to embed JSON data in a RazorEngineService compiled page
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string AsBase64(this string input)
    {
      return Convert.ToBase64String(Encoding.GetEncoding(28591).GetBytes(input));
    }

    /// <summary>
    /// Use TryGetValue and return the value, or the default
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="input"></param>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static T2 Get<T1, T2>(this Dictionary<T1, T2> input, T1 key, T2 defaultValue)
    {
      if (input.TryGetValue(key, out var value))
      {
        return value;
      }

      return defaultValue;
    }

    public static string FixSiteUrl(this string input)
    {
      return input?.Replace(":444", "");
    }

    /// <summary>
    ///   Split using a single separator
    /// </summary>
    public static string[] SplitWithString(this string input, string separator,
      StringSplitOptions stringSplitOptions =
        StringSplitOptions.RemoveEmptyEntries)
    {
      return input == null
        ? null
        : input.Split(new[] { separator }, stringSplitOptions);
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
    ///   Fill template with named items
    /// </summary>
    /// <typeparam name="T"> </typeparam>
    /// <param name="input"> </param>
    /// <param name="value"> </param>
    /// <returns> </returns>
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
      var answers = values.Select(input.FilledWithObject);
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


    /// <Summary>Returns a true bool?, or null if false</Summary>
    /// <param name="input">Input value</param>
    /// <remarks>Used mainly for exporting to xml documents</remarks>
    public static bool? OnlyIfTrue(this bool? input)
    {
      return input.HasValue && input.Value ? (bool?)true : null;
    }

    /// <Summary>Returns a false bool? if the input is false, or null if it is true</Summary>
    /// <param name="input">Input value</param>
    /// <remarks>Used mainly for exporting to xml documents</remarks>
    public static bool? OnlyIfFalse(this bool? input)
    {
      return input.HasValue && input.Value ? null : (bool?)false;
    }

    /// <Summary>Returns a true bool?, or null if false</Summary>
    /// <param name="input">Input value</param>
    /// <remarks>Used mainly for exporting to xml documents</remarks>
    public static bool? OnlyIfTrue(this bool input)
    {
      return input ? (bool?)true : null;
    }

    /// <Summary>Returns null if this is null or an empty string</Summary>
    public static string OnlyIfHasContent(this string input)
    {
      return input.HasContent() ? input : null;
    }

    /// <Summary>Returns a false bool? if the input is false, or null if it is true</Summary>
    /// <param name="input">Input value</param>
    /// <remarks>Used mainly for exporting to xml documents</remarks>
    public static bool? OnlyIfFalse(this bool input)
    {
      return input ? null : (bool?)false;
    }

    /// <Summary>Returns true if this bool? is true</Summary>
    // public static bool AsBoolean(this bool? input, bool? defaultValue = null)
    // {
    //   return input ?? defaultValue.HasValue && defaultValue.Value;
    // }

    public static bool AsBoolean(this object input, bool defaultValue = false)
    {
      if (input == null) return defaultValue;

      if (input is bool b)
      {
        return b;
      }

      var inputStr = input.ToString().ToLower();
      if (inputStr.HasNoContent()) return defaultValue;

      switch (inputStr)
      {
        case "1":
        case "y":
        case "yes":
        case "true":
          return true;

        case "0":
        case "false":
        default:
          return false;
      }
    }

    public static Guid AsGuid(this Guid? input)
    {
      return input ?? Guid.Empty;
    }

    public static Guid AsGuid(this string input)
    {
      return Guid.TryParse(input, out var guid) ? guid : Guid.Empty;
    }

    public static HtmlString AsHtmlString(this DateTime input, string format = "d MMMM yyyy")
    {
      if (input == DateTime.MinValue)
      {
        return "".AsRawHtml();
      }
      return input.AsUtc().ToString(format).AsRawHtml();
    }

    public static HtmlString AsHtmlString(this DateTime? input, string format = "d MMMM yyyy")
    {
      return input.HasValue ? input.Value.AsUtc().AsHtmlString(format) : "".AsRawHtml();
    }

    public static string AsString(this DateTime? input, string format = "d MMMM yyyy")
    {
      return input.HasValue ? input.Value.AsUtc().ToString(format) : "";
    }

    public static string AsString(this DateTime input, string format = "d MMMM yyyy")
    {
      return input != DateTime.MinValue ? input.AsUtc().ToString(format) : "";
    }

    public static Guid? AsNullableGuid(this Guid input)
    {
      if (input == Guid.Empty) return null;

      return input;
    }

    public static Guid? AsNullableGuid(this string input)
    {
      var guid = input.AsGuid();
      if (guid == Guid.Empty) return null;

      return guid;
    }

    public static IEnumerable<int> AsInts(this IEnumerable<string> input)
    {
      return input.Select(s => s.AsInt());
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

    public static long AsLong(this object input, long defaultValue = 0)
    {
      if (input == null)
        return defaultValue;
      if (input == DBNull.Value)
        return defaultValue;

      try
      {
        return (long)Math.Truncate(Convert.ToDouble(input));
      }
      catch (Exception)
      {
        return defaultValue;
      }
    }

    public static IEncodedString PercentInSpan(this int num, int total, int decimals = 0, bool surroundWithParen = false,
      bool showZero = true)
    {
      return new RawString("<span class=pct>" + num.PercentOf(total, decimals, surroundWithParen, showZero) + "</span>");
    }

    /// <summary>
    /// </summary>
    /// <param name="num"></param>
    /// <param name="total"></param>
    /// <param name="decimals">If negative, will be the maximum number of decimals, with no trailing 0 digits</param>
    /// <param name="surroundWithParen"></param>
    /// <param name="showZero"></param>
    /// <returns></returns>
    public static string PercentOf(this int num, int total, int decimals = 0, bool surroundWithParen = false,
      bool showZero = true)
    {
      if (total == 0 || num == 0 && !showZero) return "-";

      var isMax = decimals < 0;
      decimals = Math.Abs(decimals);

      var pct = Math.Round(num * 100.0 / total, decimals, MidpointRounding.ToEven);

      var numRaw = pct.ToString("F" + decimals);
      while (isMax && decimals > 0)
      {
        decimals--;
        var test = pct.ToString("F" + decimals);
        if (Convert.ToDouble(test) == pct)
        {
          numRaw = test;
        }
      }
      var s = numRaw + "%";
      return surroundWithParen ? "(" + s + ")" : s;
    }

    public static object ForSqlParameter(this string input)
    {
      if (input == null) return DBNull.Value;
      return input;
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

      if (values.Length == 1)
      {
        var value = values[0];
        if (value != null && value.GetType().Namespace != "System")
        {
          return input.FilledWithObject(value);
        }
      }

      return string.Format(input, values);
    }

    /// <summary>
    ///   Return the URL to the content file, with a version number based on the timestamp.
    /// </summary>
    public static string AsClientFileWithVersion(this string contentFilePath, string productionNameModifier = "",
      string debuggingNameModifier = "")
    {
      var useProductionFiles = ConfigurationManager.AppSettings["UseProductionFiles"].AsBoolean(); //TODO: move to config

      if (productionNameModifier.HasContent() || debuggingNameModifier.HasContent())
      {
        contentFilePath =
          contentFilePath.FilledWith(useProductionFiles ? productionNameModifier : debuggingNameModifier);
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

      var shortVersion = trimmed.Length <= sizeToUse
        ? trimmed
        : trimmed.Substring(trimmed.Length - sizeToUse, sizeToUse);
      return VirtualPathUtility.ToAbsolute(contentFilePath) + "?v=" + shortVersion;
    }

    /// <summary>
    /// Return just the first characters of the string. If maxLength is longer than the string, the whole string is returned.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public static string Left(this string input, int maxLength)
    {
      input = input ?? "";
      return input.Length <= maxLength ? input : input.Substring(0, maxLength);
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
    public static string JoinedAsString(this IEnumerable<string> list, char separator)
    {
      return JoinedAsString(list, separator.ToString(), false);
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
        : string.Join(separator,
          list2.Where(s => !skipBlanks || s.HasContent())
            .Select(s => itemLeft + s + itemRight)
            .ToArray());
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


    /// <summary>
    ///   Add new item to the end of the enumeration
    /// </summary>
    public static IEnumerable<T> AddTo<T>(this IEnumerable<T> input, List<T> addToThis)
    {
      var list = input.ToList();
      addToThis.AddRange(list);
      return list;
    }

    /// <summary>
    ///   Get a named object from Session.
    /// </summary>
    /// <typeparam name="T"> The type of the stored object </typeparam>
    /// <param name="input"> Name in Session </param>
    /// <param name="defaultValue"> Default value to use if nothing found </param>
    /// <returns> </returns>
    [DebuggerStepThrough]
    public static T FromSession<T>(this string input, T defaultValue)
    {
      //if (UserSession.CurrentContext == null || CurrentContext.Session == null) return defaultValue;
      try
      {
        var value = UserSession.CurrentContext.Session[input];
        if (value == null || value.GetType() != typeof(T))
        {
          return defaultValue;
        }
        return (T)value;
      }
      catch (Exception)
      {
        return defaultValue;
      }
    }

    /// <summary>
    ///   Get a named object from Page Items.
    /// </summary>
    /// <typeparam name="T"> The type of the stored object </typeparam>
    /// <param name="input"> Name in list </param>
    /// <param name="defaultValue"> Default value to use if nothing found </param>
    /// <param name="saveDefault">If yes, the Default value is stored if it is used</param>
    /// <returns> </returns>
    public static T FromPageItems<T>(this string input, T defaultValue, bool saveDefault = false)
    {
      var value = UserSession.CurrentContext.Items[input];
      if (value == null || value.GetType() != typeof(T))
      {
        if (saveDefault)
        {
          UserSession.CurrentContext.Items[input] = defaultValue;
        }
        return defaultValue;
      }
      return (T)value;
    }

    public static T SetInSession<T>(this string input, T newValue)
    {
      UserSession.CurrentContext.Session[input] = newValue;
      return newValue;
    }

    public static T SetInPageItems<T>(this string input, T newValue)
    {
      UserSession.CurrentContext.Items[input] = newValue;
      return newValue;
    }


    /// <summary>
    ///   If input is empty, use <paramref name="defaultValue" />
    /// </summary>
    public static string DefaultTo(this object input, object defaultValue)
    {
      if (input == null) return defaultValue.ToString();

      return input.ToString().HasNoContent() ? defaultValue.ToString() : input.ToString();
    }

    /// <summary>
    ///   If input is empty, use <paramref name="defaultValue" />
    /// </summary>
    public static string DefaultTo(this string input, string defaultValue)
    {
      return input.HasNoContent() ? defaultValue : input;
    }

    /// <summary>
    ///   If input is 0, use <paramref name="defaultValue" />
    /// </summary>
    public static int DefaultTo(this int input, int defaultValue)
    {
      return input == 0 ? defaultValue : input;
    }

    /// <summary>
    ///   If input is 0, use <paramref name="defaultValue" />
    /// </summary>
    public static int DefaultTo(this int? input, int defaultValue)
    {
      return input.HasValue && input.Value != 0 ? input.Value : defaultValue;
    }


    public static string QuotedForJavascript(this string input)
    {
      return String.Format("\"{0}\"", input
        .CleanedForJavascriptStrings()
        .Replace("\"", "\\\"")
        );
    }

    public static string QuotedForJavascript(this bool input)
    {
      return input.ToString().ToLower();
    }

    //public static string CleanedForSearching(this string input)
    //{
    //  if (input.HasNoContent()) return "";
    //  return Regex.Replace(input, @"[^\w\.\'\- ]", "");
    //}

    public static string CleanedForErrorMessages(this string input)
    {
      return HttpUtility.HtmlEncode(input ?? "");
    }

    /// <Summary>Prepare to be embedded into a javascript string</Summary>
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
    ///   Converts this object to a JSON string
    /// </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
    public static string SerializedAsJsonString(this object input)
    {
      return JsonConvert.SerializeObject(input, Formatting.None, _jsonSerializerSettings);
      //
      // return new JavaScriptSerializer
      // {
      //   MaxJsonLength = int.MaxValue
      // }.Serialize(input);
    }

    /// <summary>
    ///   Wrap object for returning to client as a JsonResult
    /// </summary>
    /// <param name="input"> </param>
    /// <param name="behavior"> Behavior to use, usually only POST is allowed. </param>
    /// <returns> </returns>
    public static JsonResult AsJsonResult(this object input,
      JsonRequestBehavior behavior = JsonRequestBehavior.DenyGet)
    {
      var custom = new NewtonSoftBasedJsonResult
      {
        // allow client full control over reading response (don't send as JSON type)
        ContentType = "text/plain",
        Data = input,
        JsonRequestBehavior = behavior,
        MaxJsonLength = int.MaxValue,
      };
      return custom;
      //
      // var jsonResult = new JsonResult
      // {
      //   ContentType = "text/plain",
      //   // allow client full control over reading response (don't send as JSON type)
      //   Data = input,
      //   JsonRequestBehavior = behavior,
      //   MaxJsonLength = int.MaxValue
      // };
      // return jsonResult;
    }

    public static TimeSpan seconds(this int input)
    {
      return new TimeSpan(0, 0, input);
    }

    public static TimeSpan minutes(this int input)
    {
      return new TimeSpan(0, input, 0);
    }

    /// <Summary>Get most inner exception</Summary>
    public static Exception LastException(this Exception input)
    {
      if (input == null) return null;
      if (input.InnerException == null) return input;
      return input.InnerException.LastException();
    }

    public static string GetAllMsgs(this Exception input, string sep)
    {
      if (input == null) return "";

      var list = new List<string>
      {
        input.Message,
        input.InnerException.GetAllMsgs(sep)
      };
      return list.JoinedAsString(sep, true);
    }

    public static string AsString(this byte[] input)
    {
      return AsString(input, Encoding.Default);
    }

    /// <Summary>Copy byte array using this codepage.</Summary>
    /// <remarks>
    ///   See http://msdn.microsoft.com/en-us/library/system.text.encoding.aspx for values
    /// </remarks>
    public static string AsString(this byte[] input, int codePage)
    {
      return Encoding.GetEncoding(codePage).GetString(input);
    }

    public static string AsString(this byte[] input, int? codePage)
    {
      return Encoding.GetEncoding(codePage.DefaultTo(1252)).GetString(input);
    }

    public static string AsString(this byte[] input, Encoding encoding)
    {
      return encoding.GetString(input);
    }

    /// <summary>
    ///   Returns <paramref name="pluralOrZero" /> if input is not 1, empty string if it is.
    /// </summary>
    /// <param name="input"> </param>
    /// <param name="pluralOrZero"> </param>
    public static string Plural(this int input, string pluralOrZero = "s")
    {
      return Plural(input, pluralOrZero, string.Empty);
    }

    /// <summary>
    ///   Returns <paramref name="pluralOrZero" /> if input is not 1,
    ///   <param name="single" />
    ///   if it is.
    /// </summary>
    /// <param name="input"> </param>
    /// <param name="pluralOrZero"> </param>
    /// <param name="single"> </param>
    public static string Plural(this int input, string pluralOrZero, string single)
    {
      return Plural(input, pluralOrZero, single, pluralOrZero);
    }

    /// <summary>
    ///   Returns <paramref name="plural" /> if input is > 1, <paramref name="single" /> if it is 1, <paramref name="zero" />
    ///   if it is 0.
    /// </summary>
    /// <param name="input"> </param>
    /// <param name="plural"> </param>
    /// <param name="single"> </param>
    /// <param name="zero"> </param>
    public static string Plural(this int input, string plural, string single, string zero)
    {
      switch (input)
      {
        case 0:
          return zero;
        case 1:
          return single;
        default:
          return plural;
      }
    }


    /// <Summary>Return the string without accents.</Summary>
    /// <remarks>
    ///   Adapted from http://blogs.msdn.com/b/michkap/archive/2007/05/14/2629747.aspx
    /// </remarks>
    public static string WithoutDiacritics(this string input, bool toLower = false)
    {
      var normalized = input.Normalize(NormalizationForm.FormD);
      var sb = new StringBuilder();

      foreach (
        var ch in
          from n in normalized
          let uc = CharUnicodeInfo.GetUnicodeCategory(n)
          where uc != UnicodeCategory.NonSpacingMark
          select n)
      {
        sb.Append(ch);
      }

      var withoutDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);

      return toLower ? withoutDiacritics.ToLowerInvariant() : withoutDiacritics;
    }
    //
    // /// <summary>
    // ///   Simple convert of <see cref="Person" /> to <see cref="SearchResult" />
    // /// </summary>
    // /// <param name="input"></param>
    // /// <param name="matchType"></param>
    // /// <param name="voteHelper"></param>
    // /// <param name="forBallot"></param>
    // /// <returns></returns>
    // public static IEnumerable<SearchResult> AsSearchResults(this IEnumerable<Person> input, int matchType,
    //   VoteHelper voteHelper, bool forBallot)
    // {
    //   return input.Select(p => p.AsSearchResult(matchType, voteHelper, forBallot));
    // }
    //
    // public static SearchResult AsSearchResult(this Person p, int matchType, VoteHelper voteHelper, bool forBallot)
    // {
    //   var canReceiveVotes = p.CanReceiveVotes.AsBoolean(true);
    //   return new SearchResult
    //   {
    //     Id = p.C_RowId,
    //     PersonGuid = p.PersonGuid,
    //     Name = p.FullNameAndArea,
    //     CanReceiveVotes = canReceiveVotes,
    //     CanVote = p.CanVote.AsBoolean(true),
    //     Ineligible = forBallot && canReceiveVotes ? null : p.IneligibleReasonGuid,
    //     //   voteHelper.IneligibleToReceiveVotes(p.IneligibleReasonGuid, p.CanReceiveVotes, forBallot),
    //     RowVersion = p.C_RowVersionInt.HasValue ? p.C_RowVersionInt.Value : 0,
    //     BestMatch = 0, // count of votes
    //     MatchType = matchType
    //   };
    // }

    /// <summary>
    ///   Replace non-characters with
    ///   <param name="sep"></param>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="sep">Character to use</param>
    /// <returns></returns>
    public static string ReplacePunctuation(this string input, char sep)
    {
      var array = input.ToCharArray();

      return new string(array.Select(c => char.IsLetterOrDigit(c) ? c : sep) // remove all punctuation
        .ToArray());
    }


    public static IEnumerable<TResult> JoinMatchingOrNull<TSource, TInner, TKey, TResult>(
      this IEnumerable<TSource> source, IEnumerable<TInner> other, Func<TSource, TKey> func, Func<TInner, TKey> innerkey,
      Func<TSource, TInner, TResult> res)
    {
      return from f in source
             join b in other on func.Invoke(f) equals innerkey.Invoke(b) into g
             from result in g.DefaultIfEmpty()
             select res.Invoke(f, result);
    }

    public static EmailAddress AsSendGridEmailAddress(this MailAddress input)
    {
      return new EmailAddress(input.Address, input.DisplayName);
    }

    public static DateTime ChopToMinute(this DateTime input)
    {
      return new DateTime(input.Year, input.Month, input.Day, input.Hour, input.Minute, 0);
    }

    /// <summary>
    /// Mark this date as a UTC time
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static DateTime AsUtc(this DateTime input)
    {
      return DateTime.SpecifyKind(input, DateTimeKind.Utc);
    }
    public static DateTime? AsUtc(this DateTime? input)
    {
      return input?.AsUtc();
    }

    // /// <summary>
    // /// Mark this date as a local time
    // /// </summary>
    // /// <param name="input"></param>
    // /// <returns></returns>
    // public static DateTime AsLocal(this DateTime input)
    // {
    //   return DateTime.SpecifyKind(input, DateTimeKind.Local);
    // }
    // public static DateTime? AsLocal(this DateTime? input)
    // {
    //   return input?.AsLocal();
    // }

    // public static LoginViewModel AsLogOnModel(this LogOnModelV1 input)
    // {
    //   return new LoginViewModel
    //   {
    //     Email = input.UserName,
    //     Password = input.PasswordV1,
    //     //        RememberMe = input.RememberMe
    //   };
    // }

    /// <summary>
    /// Convert string to matching enum. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static T AsEnum<T>(this string input, T defaultValue)
    {
      var enumType = typeof(T);
      if (!enumType.IsEnum)
      {
        throw new ArgumentException(enumType + " is not an enumeration.");
      }

      // abort if no value given
      if (string.IsNullOrEmpty(input))
      {
        return defaultValue;
      }

      // see if the text is valid for this enumeration (case sensitive)
      if (Enum.IsDefined(enumType, input))
      {
        return (T)Enum.Parse(enumType, input);
      }

      if (int.TryParse(input, out var asInt))
      {
        if (Enum.IsDefined(enumType, asInt))
        {
          return (T)Enum.Parse(enumType, asInt.ToString());
        }
      }

      // see if the text is valid for this enumeration (case insensitive)
      var names = Enum.GetNames(enumType);
      if (Array.IndexOf(names, input) != -1)
      {
        // case insensitive...
        return (T)Enum.Parse(enumType, input, true);
      }

      // do partial matching...
      var match = names.FirstOrDefault(name => name.StartsWith(input, StringComparison.InvariantCultureIgnoreCase));
      if (match != null)
      {
        return (T)Enum.Parse(enumType, match);
      }

      // didn't find one
      return defaultValue;
    }

    public static string InSentence(this IEnumerable<string> input, string andOr)
    {
      var words = input.ToList();
      switch (words.Count)
      {
        case 1:
          return words[0];
        case 2:
          return words.JoinedAsString($" {andOr} ");
        default:
          var words2 = words.Take(words.Count - 1);
          return words2.JoinedAsString(", ")
                 + $", {andOr} {words.Last()}";
      }
    }

    public static string GetLinesAfterSkipping(this string input, int numFirstRowsToSkip)
    {
      var row = 0;
      var stringBuilder = new StringBuilder(input.Length);
      const string crlf = "\r\n";
      using (var sr = new StringReader(input))
      {
        string line;
        while ((line = sr.ReadLine()) != null)
        {
          row++;
          if (row <= numFirstRowsToSkip)
          {
            continue;
          }

          if (stringBuilder.Length > 0)
          {
            stringBuilder.Append(crlf);
          }
          stringBuilder.Append(line);
        }
      }

      return stringBuilder.ToString();
    }


    public static string FilteredStackTrace(this string stackTrace)
    {
      var parts = stackTrace.Split(new[] { '\n', '\r' }).Where(s => !string.IsNullOrEmpty(s)).Reverse().ToList();
      var newParts = new List<string>();
      var foundOurCode = false;
      foreach (var part in parts)
      {
        if (part.Contains("at TallyJ."))
        {
          foundOurCode = true;
        }
        if (foundOurCode)
        {
          newParts.Add(part);
        }
      }
      return newParts.Select(s => s).Reverse().JoinedAsString("\r\n");
    }

    /// <summary>
    /// Split characters and add spaces between:  "1235" becomes "1 2 3 5"
    /// </summary>
    /// <param name="input"></param>
    /// <param name="space"></param>
    /// <returns></returns>
    public static string AddSpaces(this string input, string space = " ")
    {
      var chars = input.ToCharArray().Select(c => c.ToString());
      return chars.JoinedAsString(space);
    }
  }
}