using System.Collections;
using System.Web;

namespace Site.Code.UnityRelated
{
	public class HttpContextItemsProvider : IContextItemsProvider
	{
		public IDictionary Items
		{
			get
			{
				return HttpContext.Current.Items;
			}

		}
	}
}