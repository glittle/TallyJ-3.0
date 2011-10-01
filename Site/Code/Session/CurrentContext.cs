using System.Collections;
using System.Web;
using System.Web.SessionState;

namespace TallyJ.Code.Session
{
	/// <summary>
	/// Access to HttpContext.
	/// </summary>
	/// <remarks>Can be extended to support Testing environment.</remarks>
	public static class CurrentContext
	{
		public static IDictionary Items
		{
			get { return HttpContext.Current.Items; }
		}

		public static HttpSessionState Session
		{
			get { return HttpContext.Current.Session; }
		}
	}
}