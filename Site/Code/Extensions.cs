using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using RazorEngine.Text;
using TallyJ.CoreModels;
using TallyJ.EF;

namespace TallyJ.Code
{
  public static class Extensions
  {
    /// <summary>
    ///     Not IsNullOrEmpty
    /// </summary>
    public static bool HasContent(this string input)
    {
      return !string.IsNullOrEmpty(input);
    }

    /// <Summary>Whether this guid is the Empty guid or not</Summary>
    public static bool HasContent(this Guid input)
    {
      return input != Guid.Empty;
    }

    /// <summary>
    ///     Return true if the input is empty or null.
    /// </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
    public static bool HasNoContent(this string input)
    {
      return string.IsNullOrEmpty(input);
    }

    public static bool HasNoContent(this int? input)
    {
      return !input.HasValue || input.Value == 0;
    }

    /// <summary>
    ///     Format for display in an MVC page.
    /// </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
    public static HtmlString AsRawHtml(this string input)
    {
      return new HtmlString(input);
    }

    /// <summary>
    ///     Format for display in an MVC page.
    /// </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
    public static MvcHtmlString AsRawMvcHtml(this string input)
    {
      return new MvcHtmlString(input);
    }


    /// <summary>
    ///     Split using a single separator
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
    ///     Use the input string as the format with string.Format
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
    ///     Fill template with named items
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

    /// <Summary>Returns true if this bool? is true</Summary>
    public static bool AsBoolean(this bool? input, bool? defaultValue = null)
    {
      return input.HasValue ? input.Value : (defaultValue.HasValue && defaultValue.Value);
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

    public static bool AsBoolean(this string input)
    {
      if (input.HasNoContent()) return false;

      switch (input)
      {
        case "1":
        case "true":
        case "True":
          return true;

        case "0":
        case "false":
        case "False":
        default:
          return false;
      }
    }

    public static Guid AsGuid(this Guid? input)
    {
      return input.HasValue ? input.Value : Guid.Empty;
    }

    public static Guid AsGuid(this string input)
    {
      Guid guid;
      if (Guid.TryParse(input, out guid))
      {
        return guid;
      }
      return Guid.Empty;
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

    public static IEncodedString PercentInSpan(this int num, int total, int decimals = 0, bool surroundWithParen = false, bool showZero = true)
    {
      return new RawString("<span class=pct>" + num.PercentOf(total, decimals, surroundWithParen, showZero) + "</span>");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="num"></param>
    /// <param name="total"></param>
    /// <param name="decimals">If negative, will be the maximum number of decimals, with no trailing 0 digits</param>
    /// <param name="surroundWithParen"></param>
    /// <param name="showZero"></param>
    /// <returns></returns>
    public static string PercentOf(this int num, int total, int decimals = 0, bool surroundWithParen = false, bool showZero = true)
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
    ///     Use the input string as the format with string.Format
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
        if (value.GetType().Namespace != "System")
        {
          return input.FilledWithObject(value);
        }
      }

      return string.Format(input, values);
    }

    /// <summary>
    ///     Return the URL to the content file, with a version number based on the timestamp.
    /// </summary>
    public static string AsClientFileWithVersion(this string contentFilePath, string productionNameModifier = "",
                                                 string debuggingNameModifier = "")
    {
      var UseDebugFiles = true; //TODO: move to config

      if (productionNameModifier.HasContent() || debuggingNameModifier.HasContent())
      {
        contentFilePath =
            contentFilePath.FilledWith(UseDebugFiles ? debuggingNameModifier : productionNameModifier);
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
    ///     For an enumeration of strings, join them.
    /// </summary>
    public static string JoinedAsString(this IEnumerable<string> list)
    {
      return JoinedAsString(list, string.Empty);
    }

    /// <summary>
    ///     For an enumeration of strings, join them.
    /// </summary>
    public static string JoinedAsString(this IEnumerable<string> list, string separator)
    {
      return JoinedAsString(list, separator, false);
    }

    /// <summary>
    ///     For an enumeration of strings, join them.
    /// </summary>
    public static string JoinedAsString(this IEnumerable<string> list, string separator, bool skipBlanks)
    {
      return list.JoinedAsString(separator, string.Empty, string.Empty, skipBlanks);
    }

    /// <summary>
    ///     For an enumeration of strings, join them. Each item has itemLeft and itemRight added.
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
                                    .
                                     ToArray());
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
    ///     Surround with left and right strings. If the input has no content, an empty string is returned.
    /// </summary>
    public static string SurroundContentWith(this string input, string left, string right)
    {
      if (input.HasNoContent())
        return string.Empty;

      return left + input + right;
    }


    /// <summary>
    ///     Add new item to the end of the enumeration
    /// </summary>
    public static IEnumerable<T> AddTo<T>(this IEnumerable<T> input, List<T> addToThis)
    {
      var list = input.ToList();
      addToThis.AddRange(list);
      return list;
    }

    /// <summary>
    ///     Get a named object from Session.
    /// </summary>
    /// <typeparam name="T"> The type of the stored object </typeparam>
    /// <param name="input"> Name in Session </param>
    /// <param name="defaultValue"> Default value to use if nothing found </param>
    /// <returns> </returns>
    [DebuggerStepThrough]
    public static T FromSession<T>(this string input, T defaultValue)
    {
      if (HttpContext.Current == null || HttpContext.Current.Session == null) return defaultValue;
      try
      {
        var value = HttpContext.Current.Session[input];
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
    ///     Get a named object from Page Items.
    /// </summary>
    /// <typeparam name="T"> The type of the stored object </typeparam>
    /// <param name="input"> Name in list </param>
    /// <param name="defaultValue"> Default value to use if nothing found </param>
    /// <param name="saveDefault">If yes, the Default value is stored if it is used</param>
    /// <returns> </returns>
    public static T FromPageItems<T>(this string input, T defaultValue, bool saveDefault = false)
    {
      var value = HttpContext.Current.Items[input];
      if (value == null || value.GetType() != typeof(T))
      {
        if (saveDefault)
        {
          HttpContext.Current.Items[input] = defaultValue;
        }
        return defaultValue;
      }
      return (T)value;
    }

    public static T SetInSession<T>(this string input, T newValue)
    {
      HttpContext.Current.Session[input] = newValue;
      return newValue;
    }

    public static T SetInPageItems<T>(this string input, T newValue)
    {
      HttpContext.Current.Items[input] = newValue;
      return newValue;
    }


    /// <summary>
    ///     If input is empty, use <paramref name="defaultValue" />
    /// </summary>
    public static string DefaultTo(this object input, object defaultValue)
    {
      if (input == null) return defaultValue.ToString();

      return input.ToString().HasNoContent() ? defaultValue.ToString() : input.ToString();
    }

    /// <summary>
    ///     If input is empty, use <paramref name="defaultValue" />
    /// </summary>
    public static string DefaultTo(this string input, string defaultValue)
    {
      return input.HasNoContent() ? defaultValue : input;
    }

    /// <summary>
    ///     If input is 0, use <paramref name="defaultValue" />
    /// </summary>
    public static int DefaultTo(this int input, int defaultValue)
    {
      return input == 0 ? defaultValue : input;
    }

    /// <summary>
    ///     If input is 0, use <paramref name="defaultValue" />
    /// </summary>
    public static int DefaultTo(this int? input, int defaultValue)
    {
      return (input.HasValue && input.Value == 0) ? input.Value : defaultValue;
    }


    public static string QuotedForJavascript(this string input)
    {
      return String.Format("\"{0}\"", input
                                          .CleanedForJavascriptStrings()
                                          .Replace("\"", "\\\"")
          );
    }

    //public static string CleanedForSearching(this string input)
    //{
    //  if (input.HasNoContent()) return "";
    //  return Regex.Replace(input, @"[^\w\.\'\- ]", "");
    //}

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
    ///     Converts this object to a JSON string
    /// </summary>
    /// <param name="input"> </param>
    /// <returns> </returns>
    public static string SerializedAsJsonString(this object input)
    {
      return new JavaScriptSerializer().Serialize(input);
    }


    /// <summary>
    ///     Wrap object for returning to client as a JsonResult
    /// </summary>
    /// <param name="input"> </param>
    /// <param name="behavior"> Behavior to use, usually only POST is allowed. </param>
    /// <returns> </returns>
    public static JsonResult AsJsonResult(this object input,
                                          JsonRequestBehavior behavior = JsonRequestBehavior.DenyGet)
    {
      var jsonResult = new JsonResult
          {
            ContentType = "text/plain",
            // allow client full control over reading response (don't send as JSON type)
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
    ///     See http://msdn.microsoft.com/en-us/library/system.text.encoding.aspx for values
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
    ///     Returns <paramref name="pluralOrZero" /> if input is not 1, empty string if it is.
    /// </summary>
    /// <param name="input"> </param>
    /// <param name="pluralOrZero"> </param>
    public static string Plural(this int input, string pluralOrZero = "s")
    {
      return Plural(input, pluralOrZero, string.Empty);
    }

    /// <summary>
    ///     Returns <paramref name="pluralOrZero" /> if input is not 1,
    ///     <param name="single" />
    ///     if it is.
    /// </summary>
    /// <param name="input"> </param>
    /// <param name="pluralOrZero"> </param>
    /// <param name="single"> </param>
    public static string Plural(this int input, string pluralOrZero, string single)
    {
      return Plural(input, pluralOrZero, single, pluralOrZero);
    }

    /// <summary>
    ///     Returns <paramref name="plural" /> if input is > 1, <paramref name="single" /> if it is 1, <paramref name="zero" /> if it is 0.
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

    //    public static IEnumerable<Vote> AsVotes(this IEnumerable<VoteInfo> inputs)
    //    {
    //      return inputs.Select(VoteInfo => new Vote
    //          {
    //            C_RowId = VoteInfo.VoteId,
    //            BallotGuid = VoteInfo.BallotGuid,
    //            C_RowVersion = null,
    //            InvalidReasonGuid = VoteInfo.VoteIneligibleReasonGuid,
    //            PersonCombinedInfo = VoteInfo.PersonCombinedInfoInVote,
    //            PersonGuid = VoteInfo.PersonGuid,
    //            PositionOnBallot = VoteInfo.PositionOnBallot,
    //            SingleNameElectionCount = VoteInfo.SingleNameElectionCount,
    //            StatusCode = VoteInfo.VoteStatusCode
    //          });
    //    }

    /// <Summary>Return the string without accents.</Summary>
    /// <remarks>
    ///     Adapted from http://blogs.msdn.com/b/michkap/archive/2007/05/14/2629747.aspx
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

    /// <summary>
    /// Simple convert of <see cref="Person"/> to <see cref="SearchResult"/> 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="matchType"></param>
    /// <param name="voteHelper"></param>
    /// <param name="forBallot"></param>
    /// <returns></returns>
    public static IEnumerable<SearchResult> AsSearchResults(this IEnumerable<Person> input, int matchType, VoteHelper voteHelper, bool forBallot)
    {
      return input.Select(p => p.AsSearchResult(matchType, voteHelper, forBallot));
    }

    public static SearchResult AsSearchResult(this Person p, int matchType, VoteHelper voteHelper, bool forBallot)
    {
      var canReceiveVotes = p.CanReceiveVotes.AsBoolean(true);
      return new SearchResult
      {
        Id = p.C_RowId,
        PersonGuid = p.PersonGuid,
        Name = p.FullNameAndArea,
        CanReceiveVotes = canReceiveVotes,
        CanVote = p.CanVote.AsBoolean(true),
        Ineligible = forBallot && canReceiveVotes ? null : p.IneligibleReasonGuid, //   voteHelper.IneligibleToReceiveVotes(p.IneligibleReasonGuid, p.CanReceiveVotes, forBallot),
        RowVersion = p.C_RowVersionInt.HasValue ? p.C_RowVersionInt.Value : 0,
        BestMatch = 0, // count of votes
        MatchType = matchType
      };
    }

    /// <summary>
    /// Replace non-characters with <param name="sep"></param>
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


    public static IEnumerable<TResult> JoinMatchingOrNull<TSource, TInner, TKey, TResult>(this IEnumerable<TSource> source, IEnumerable<TInner> other, Func<TSource, TKey> func, Func<TInner, TKey> innerkey, Func<TSource, TInner, TResult> res)
    {
      return from f in source
             join b in other on func.Invoke(f) equals innerkey.Invoke(b) into g
             from result in g.DefaultIfEmpty()
             select res.Invoke(f, result);
    }
  }
}