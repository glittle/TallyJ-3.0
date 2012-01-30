using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace TallyJ.Code
{
  public static class LogHelper
  {
    public static void Add(string message)
    {
      AddToLog(new C_Log
                 {
                   ElectionGuid = UserSession.CurrentElectionGuid,
                   ComputerCode = UserSession.CurrentComputerCode,
                   LocationGuid = UserSession.CurrentLocationGuid,
                   Details = message
                 });
    }

    private static void AddToLog(C_Log logItem)
    {
      var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;
      db.C_Log.Add(logItem);
      db.SaveChanges();
    }
  }
}