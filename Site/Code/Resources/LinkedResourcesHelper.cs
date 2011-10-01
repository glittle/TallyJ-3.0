using System;

namespace Site.Code.Resources
{
	public class LinkedResourcesHelper : ILinkedResourcesHelper
	{
		#region ILinkedResourcesHelper Members

		/// <summary>Create a link tag for this css file</summary>
		/// <param name="itemInfo">The name (without path) of the CSS file.  Can append media type with |. </param>
		/// <param name="rootPath">Folder for the CSS file. By default, this is the Content folder.</param>
		/// <example>Normal example:   main.css</example>
		/// <example>Example with media tag:   print.css|print</example>
		public string CreateStyleSheetLinkTag(string itemInfo, string rootPath)
		{
			var parts = itemInfo.SplitWithString("|", StringSplitOptions.RemoveEmptyEntries);
			var name = parts[0];
			var media = parts.Length == 1 ? string.Empty : parts[1];

			var cssPath = string.Format("{0}/{1}", rootPath, name);

			var url = cssPath.AsClientFileWithVersion();

			if (url.HasNoContent())
			{
				return null;
			}

			var mediaAttribute = media.HasContent() ? string.Format(" media=\"{0}\"", media) : string.Empty;

			return string.Format("<link href=\"{0}\"{1} rel=stylesheet type=\"text/css\">", url, mediaAttribute);
		}

		/// <summary>Create a link tag for this css file</summary>
		/// <param name="itemInfo">The name (without path) of the CSS file.  Can append media type with |. </param>
		/// <example>Normal example:   main.css</example>
		/// <example>Example with media tag:   print.css|print</example>
		public string CreateStyleSheetLinkTag(string itemInfo)
		{
			return CreateStyleSheetLinkTag(itemInfo, "~/Content");
		}

		/// <summary>Create a script tag for this js file</summary>
		/// <param name="name">The name (without path) of the js file. </param>
		/// <example>Normal example:   main.css</example>
		/// <example>Example with media tag:   print.css|print</example>
		public string CreateJavascriptSourceTag(string name)
		{
			return CreateJavascriptSourceTag(name, "~/Scripts");
		}

		/// <summary>Create a script tag for this js file</summary>
		/// <param name="name">The name (without path) of the js file. </param>
		/// <param name="rootPath">Folder for the js file. By default, this is the Scripts folder.</param>
		/// <example>Normal example:   main.css</example>
		/// <example>Example with media tag:   print.css|print</example>
		public string CreateJavascriptSourceTag(string name, string rootPath)
		{
			var path = string.Format("{0}/{1}", rootPath, name);

			var url = path.AsClientFileWithVersion();

			if (url.HasNoContent())
			{
				return null;
			}

			return string.Format("<script src=\"{0}\" type=\"text/javascript\"></script>", url);
		}

		#endregion
	}
}