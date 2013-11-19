using System.Web;
using System.Web.Mvc;

namespace TallyJ.Code.Resources
{
	public interface IViewResourcesHelper
	{
		void Register<T>(WebViewPage<T> viewPage);
        IHtmlString CreateTagsToReferenceContentFiles(string extension, params string[] secondaryExtensions);
    }
}