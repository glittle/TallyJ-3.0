using System.Web;
using System.Web.Mvc;

namespace Site.Code.Resources
{
	public interface IViewResourcesHelper
	{
		void Register<T>(WebViewPage<T> viewPage);
		IHtmlString CreateTagsToReferenceClientResourceFiles(string extension);
	}
}