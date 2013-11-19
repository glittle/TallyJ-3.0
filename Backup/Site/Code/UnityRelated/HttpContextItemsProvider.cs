using System.Collections;
using System.Web;

namespace TallyJ.Code.UnityRelated
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