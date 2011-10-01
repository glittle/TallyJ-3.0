using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace TallyJ.Code
{
	public class BaseViewModel
	{
		tallyj2dEntities _db;

		/// <summary>Access to the database</summary>
		public tallyj2dEntities DbContext
		{
			get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
		}
	}
}