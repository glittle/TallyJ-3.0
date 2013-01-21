using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public IEnumerable<string> GetTag(string virtualPath, string extension, string[] secondaryExtensions)
        {
            var list = new List<string>
        {
          extension
        };
            list.AddRange(secondaryExtensions);
            string resourceUrl;
            var found = CheckFile(virtualPath, list, out extension, out resourceUrl);

            if (!found)
            {
                yield return string.Empty;
            }
            else
            {
                var containerPath = Path.GetDirectoryName(virtualPath);
                var file = Path.GetFileName(resourceUrl);

                switch (extension.ToLower())
                {
                    case "min.css":
                    case "css":
                    case "less":
                        yield return _linkedResourcesHelper.CreateStyleSheetLinkTag(file, containerPath);
                        break;

                    case "min.js":
                    case "js":
                        yield return _linkedResourcesHelper.CreateJavascriptSourceTag(file, containerPath);
                        break;

                    default:
                        yield return string.Empty;
                        break;
                }
            }
        }

        private bool CheckFile(string virtualPath, IList<string> extensions, out string foundExtension, out string url)
        {
            if (extensions.Count == 0)
            {
                foundExtension = null;
                url = null;
                return false;
            }

            var extension = extensions[0];

            url = virtualPath + "." + extension;
            var rawPath = HttpContext.Current.Server.MapPath(url);

            bool exists;

            if (_dict.ContainsKey(rawPath))
            {
                exists = _dict[rawPath];
            }
            else
            {
                // learn about this possible file
                exists = File.Exists(rawPath);
                _dict.Add(rawPath, exists);
            }

            if (exists)
            {
                foundExtension = extension;
                return true;
            }

            return CheckFile(virtualPath, extensions.Skip(1).ToList(), out foundExtension, out url);
        }
    }
}