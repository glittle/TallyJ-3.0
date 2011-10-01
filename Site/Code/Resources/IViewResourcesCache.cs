using System.Collections.Generic;

namespace Site.Code.Resources
{
	public interface IViewResourcesCache
	{
		IEnumerable<string> GetTag(string virtualPath, string extension);
	}
}