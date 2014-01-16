using System;
using System.Web.Mvc;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.CoreModels;
using TallyJ.EF;

namespace TallyJ.Code
{
  public abstract class BaseController : Controller
	{
		TallyJ2dEntities _db;

    protected override IAsyncResult BeginExecuteCore(AsyncCallback callback, object state)
    {
      if (UserSession.CurrentElectionGuid != Guid.Empty)
      {
        new ComputerModel().RefreshLastContact();

      }
      return base.BeginExecuteCore(callback, state);
    }

    /// <summary>Access to the database</summary>
		public TallyJ2dEntities Db
		{
			get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
		}
	}
}