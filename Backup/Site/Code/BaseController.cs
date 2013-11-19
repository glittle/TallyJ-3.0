using System.Web.Mvc;
using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;
using TallyJ.Models;

namespace TallyJ.Code
{
  public abstract class BaseController : Controller
	{
		TallyJ2dContext _db;

		/// <summary>Access to the database</summary>
		public TallyJ2dContext Db
		{
			get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
		}
	}
}