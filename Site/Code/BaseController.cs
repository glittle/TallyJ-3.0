using System;
using System.Web.Mvc;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.EF;

namespace TallyJ.Code;

public abstract class BaseController : Controller
{
  private ITallyJDbContext _db;

  /// <summary>Access to the database</summary>
  protected ITallyJDbContext Db => _db ??= UserSession.GetNewDbContext;

  protected override IAsyncResult BeginExecuteCore(AsyncCallback callback, object state)
  {
    new ComputerModel().RefreshLastContact();
    //Debug.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
    return base.BeginExecuteCore(callback, state);
  }
}