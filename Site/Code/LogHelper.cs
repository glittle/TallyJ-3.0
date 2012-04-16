using System;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace TallyJ.Code
{
  public interface ILogHelper
  {
    void Add(string message);
  }

  public class LogHelper : ILogHelper
  {
    public void Add(string message)
    {
      AddToLog(new C_Log
                 {
                   ElectionGuid = UserSession.CurrentElectionGuid,
                   ComputerCode = UserSession.CurrentComputerCode,
                   LocationGuid = UserSession.CurrentLocationGuid,
                   Details = message
                 });
    }

    private void AddToLog(C_Log logItem)
    {
      var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;
      logItem.AsOf = DateTime.Now;
      db.C_Log.Add(logItem);
      db.SaveChanges();
    }
  }
}