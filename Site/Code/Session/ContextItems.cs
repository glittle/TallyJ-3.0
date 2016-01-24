using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TallyJ.Code.Resources;
using TallyJ.CoreModels;

namespace TallyJ.Code.Session
{
	public static class ContextItems
	{
    public static LocationModel LocationModel
    {
      get
      {
        var model = UserSession.CurrentContext.Items[ItemKey.LocationModel] as LocationModel;
				if (model == null)
				{
          UserSession.CurrentContext.Items[ItemKey.LocationModel] = model = new LocationModel();
				}
				return model;
      }
    }

	  public static Dictionary<string, string> JavascriptForPage
		{
			get
			{
				var dictionary = UserSession.CurrentContext.Items[ItemKey.JavascriptForPage] as Dictionary<string, string>;
				if (dictionary == null)
				{
          UserSession.CurrentContext.Items[ItemKey.JavascriptForPage] = dictionary = new Dictionary<string, string>();
				}
				return dictionary;
			}
		}

		public static Dictionary<string, string> ResourcesForJavascript
		{
			get
			{
				var dictionary = UserSession.CurrentContext.Items[ItemKey.ResourcesForJavascript] as Dictionary<string, string>;
				if (dictionary == null)
				{
          UserSession.CurrentContext.Items[ItemKey.ResourcesForJavascript] = dictionary = new Dictionary<string, string>();
				}
				return dictionary;
			}
		}

	  /// <summary>Add line(s) of javascript to be inserted into the page</summary>
	  public static void AddJavascriptForPage(string firstLine, params string[] linesOfCode)
	  {
	    AddJavascriptForPage(Guid.NewGuid().ToString(), firstLine, linesOfCode);
	  }

	  /// <summary>Add line(s) of javascript to be inserted into the page</summary>
		public static void AddJavascriptForPage(string uniqueKey, string firstLine, params string[] linesOfCode)
		{
			if (JavascriptForPage.ContainsKey(uniqueKey))
			{
				return;
			}

			JavascriptForPage.Add(uniqueKey, firstLine + linesOfCode.JoinedAsString("\n").SurroundContentWith("\\n", ""));
		}

		/// <summary>Called from the page, insert any javascript that was defined in server code.</summary>
		public static IHtmlString GetJavascriptForPage()
		{
			var resources = ResourcesForJavascript;

			var javascriptForPage = JavascriptForPage;

			if (resources.Count != 0)
			{
				var script = new List<string>
                       {
                         "$.extend(MyResources, {\n",
                         resources.Select(r => "{0}:{1}".FilledWith(r.Key, r.Value)).JoinedAsString(",\n"),
                         "});"
                       };
				javascriptForPage.Add("resources", script.JoinedAsString());
			}

			AddJavascriptForPage("jsTemplates", TemplateLoader.GetTemplates());

			// read it again, to get the new values
			javascriptForPage = JavascriptForPage;

			return javascriptForPage.Count != 0
					 ? "<script>{0}</script>".FilledWith(javascriptForPage.Values.JoinedAsString("\n")).AsRawHtml()
					 : null;
		}
	}
}