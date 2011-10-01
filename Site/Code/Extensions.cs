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
					 : input.Split(new[] { separator }, stringSplitOptions);
		}

		/// <summary>
		///   Use the input string as the format with string.Format
		/// </summary>
		public static string FilledWith(this string input, params object[] values)
		{
			if (input.HasNoContent())
				return string.Empty;
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
			return list.JoinedAsString(separator, string.Empty, string.Empty);
		}

		/// <summary>
		///   For an enumeration of strings, join them. Each item has itemLeft and itemRight added.
		/// </summary>
		public static string JoinedAsString(this IEnumerable<string> list, string separator, string itemLeft,
											string itemRight)
		{
			return list == null || list.Count() == 0
					 ? string.Empty
					 : string.Join(separator, list.Select(s => itemLeft + s + itemRight).ToArray());
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
		public static T FromSession<T>(this string input, T defaultValue = null) where T : class 
		{
			var value = HttpContext.Current.Session[input];
			if (value == null || value.GetType() != typeof(T))
			{
				return defaultValue;
			}
			return (T)value;
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
		public static string SerializedAsJson(this object input)
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
	}
}