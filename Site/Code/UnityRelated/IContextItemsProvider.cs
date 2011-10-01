using System.Collections;

namespace Site.Code.UnityRelated
{
	/// <summary>
	/// Lifetime manager
	/// </summary>
	/// <remarks>
	///See http://xhalent.wordpress.com/2011/01/25/wcfcontext-and-inversion-of-control-in-wcf-with-unity-and-servicelocator/#context-lifetime-managers-revisited 
	/// </remarks>

	public interface IContextItemsProvider
	{
		IDictionary Items
		{
			get;
		}
	}
}