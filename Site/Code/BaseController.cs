using System;
using System.Diagnostics;
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
    ITallyJDbContext _db;

    protected override IAsyncResult BeginExecuteCore(AsyncCallback callback, object state)
    {
      new ComputerModel().RefreshLastContact();
      Debug.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
      return base.BeginExecuteCore(callback, state);
    }

    /// <summary>Access to the database</summary>
    public ITallyJDbContext Db
    {
      get { return _db ?? (_db = UnityInstance.Resolve<IDbContextFactory>().DbContext); }
    }
  }
}