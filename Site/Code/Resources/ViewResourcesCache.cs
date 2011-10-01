using System.Collections.Generic;
using System.IO;
using System.Web;
using TallyJ.Code.UnityRelated;

namespace TallyJ.Code.Resources
{
	public class ViewResourcesCache : IViewResourcesCache
	{
		readonly Dictionary<string, bool> _dict;
		readonly ILinkedResourcesHelper _linkedResourcesHelper;

		public ViewResourcesCache()
		{
			_linkedResourcesHelper = UnityInstance.Resolve<ILinkedResourcesHelper>();
			_dict = new Dictionary<string, bool>();
		}

		#region IViewResourcesCache Members

		public IEnumerable<string> GetTag(string virtualPath, string extension)
		{
			var resourceUrl = virtualPath + "." + extension;
			var rawPath = HttpContext.Current.Server.MapPath(resourceUrl);

			if (!_dict.ContainsKey(rawPath))
			{
				// learn about this possible file
				_dict.Add(rawPath, File.Exists(rawPath));
			}

			if (!_dict[rawPath])
			{
				yield return string.Empty;
			}
			else
			{
				var containerPath = Path.GetDirectoryName(virtualPath);
				var file = Path.GetFileName(resourceUrl);

				switch (extension.ToLower())
				{
					case "css":
						yield return _linkedResourcesHelper.CreateStyleSheetLinkTag(file, containerPath);
						break;

					case "js":
						yield return _linkedResourcesHelper.CreateJavascriptSourceTag(file, containerPath);
						break;

					default:
						yield return string.Empty;
						break;
				}
			}
		}

		#endregion
	}
}