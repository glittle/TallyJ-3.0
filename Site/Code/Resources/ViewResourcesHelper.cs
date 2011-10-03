using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.Code.Resources
{
	public class ViewResourcesHelper : IViewResourcesHelper
	{
		readonly List<string> _list;

		public ViewResourcesHelper()
		{
			_list = new List<string>();
		}

		#region IViewResourcesHelper Members

		/// <summary>Register that this view has been used in this current request</summary>
		public void Register<T>(WebViewPage<T> viewPage)
		{
			var virtualPath = viewPage.VirtualPath;

			if (!_list.Contains(virtualPath))
			{
				_list.Insert(0, virtualPath);
			}

			var folderPath = VirtualPathUtility.GetDirectory(virtualPath);
			folderPath = VirtualPathUtility.Combine(folderPath, VirtualPathUtility.GetFileName(folderPath));
			if (!_list.Contains(folderPath))
			{
				_list.Insert(0, folderPath);
			}
		}

		/// <summary>From the layout page, this is called to inject links to CSS and JS files for this view</summary>
		/// <param name="extension"></param>
		public IHtmlString CreateTagsToReferenceClientResourceFiles(string extension)
		{
			var cachedPaths = UnityInstance.Resolve<IViewResourcesCache>();

			// make a local copy, so we can clear the ones we've done
			var list = _list.ToList();

		  //Debug: ContextItems.AddJavascriptForPage(new Random().Next(1, 555).ToString(), "// " + list.JoinedAsString(", "));

			var alreadySent = HttpContext.Current.Items["ClientFilesSent"] as List<string>;
			if (alreadySent == null)
			{
				HttpContext.Current.Items["ClientFilesSent"] = alreadySent = new List<string>();
			}

			var htmlString =
				list
					.SelectMany(s => cachedPaths.GetTag(s, extension))
					.Where(s => s.HasContent() && !alreadySent.Contains(s))
					.AddTo(alreadySent)
					.JoinedAsString("\n");

			return new MvcHtmlString(htmlString);
		}

		#endregion
	}
}