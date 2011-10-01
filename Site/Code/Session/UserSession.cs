using System.Web;
using TallyJ.EF;

namespace TallyJ.Code.Session
{
	public static class UserSession
	{
		/// <summary>Logged in identity name.</summary>
		public static string LoginId
		{
			get { return HttpContext.Current.User.Identity.Name ?? ""; }
		}

		/// <summary>Has this person logged in?</summary>
		public static bool IsLoggedIn
		{
			get { return LoginId.HasContent(); }
		}

		/// <summary>
		/// The current election, as stored in Session.  Could be null.
		/// </summary>
		public static Election CurrentElection
		{
			get { return SessionKey.CurrentElection.FromSession<Election>(); }
			set { HttpContext.Current.Session[SessionKey.CurrentElection] = value; }
		}

		/// <summary>
		/// Title of election, if one is selected
		/// </summary>
		public static string CurrentElectionTitle
		{
			get { return CurrentElection == null ? "[No election selected]" : CurrentElection.Title; }
		}
	}
}