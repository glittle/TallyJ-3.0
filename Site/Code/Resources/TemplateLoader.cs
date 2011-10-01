using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Caching;
using TallyJ.Code.Session;

namespace TallyJ.Code.Resources
{
	public class TemplateLoader
	{
		#region File enum

		public enum File
		{
			// This enum is manually created and maintained. Add a new item when needed.
			ElectionListItem
		}

		#endregion

		public static void RequireTemplates(params File[] templateList)
		{
			var list = HttpContext.Current.Items[ItemKey.JsTemplates] as List<File>;
			if (list == null)
			{
				HttpContext.Current.Items[ItemKey.JsTemplates] = list = new List<File>();
			}
			list.AddRange(templateList);
		}

		public static string GetTemplates()
		{
			var templateList = HttpContext.Current.Items[ItemKey.JsTemplates] as List<File>;
			if (templateList == null)
			{
				return "";
			}

			return templateList
			  .Select(templateName =>
					  string.Format("site.templates.{0}={1}", templateName,
									GetFileContent(templateName.ToString()).QuotedForJavascript()
						))
			  .JoinedAsString(";")
			  .SurroundWith(";", ";");
		}

		private static string GetFileContent(string templateName)
		{
			var nameInCache = "JsTemplate." + templateName;

			var content = HttpContext.Current.Cache[nameInCache] as string;

			if (content == null)
			{
				var path = string.Format("{0}JsTemplates\\{1}.html", new SiteInfo().RootPath, templateName.Replace("_", "\\"));

				if (!System.IO.File.Exists(path))
				{
					throw new FileNotFoundException("Can't find template " + templateName);
				}

				content = System.IO.File.ReadAllText(path);
				HttpContext.Current.Cache.Insert(nameInCache, content, new CacheDependency(path));
			}

			return content;
		}
	}
}