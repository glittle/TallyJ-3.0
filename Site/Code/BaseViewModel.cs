using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace TallyJ.Code
{
	public abstract class BaseViewModel
	{
		TallyJ2Entities _db;

		/// <summary>Access to the database</summary>
		public TallyJ2Entities DbContext
		{
			get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
		}
	}
}