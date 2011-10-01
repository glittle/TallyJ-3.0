using System.Collections.Generic;

namespace TallyJ.Code.Resources
{
	public interface IViewResourcesCache
	{
		IEnumerable<string> GetTag(string virtualPath, string extension);
	}
}