using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.EF;

namespace TallyJ.Code
{
  public interface ILogHelper
  {
    void Add(string message, bool alsoSendToRemoteLog = false);
  }

  public class LogHelper : ILogHelper
  {
    private readonly Guid _electionGuid;

    public LogHelper(Guid electionGuid)
    {
      _electionGuid = electionGuid;
    }

    public LogHelper() : this(UserSession.CurrentElectionGuid)
    {
    }

    public void Add(string message, bool alsoSendToRemoteLog = false)
    {
      AddToLog(new C_Log
      {
        ElectionGuid = _electionGuid,
        ComputerCode = UserSession.CurrentComputerCode,
        LocationGuid = UserSession.CurrentLocationGuid,
        Details = message
      });
      if (alsoSendToRemoteLog)
      {
        SendToRemoteLog(message);
      }
    }

    public void SendToRemoteLog(string message)
    {
      var iftttKey = ConfigurationManager.AppSettings["iftttKey"].DefaultTo("");
      if (iftttKey.HasNoContent())
      {
        return;
      }

      var info = new NameValueCollection();
      info["value1"] = UserSession.LoginId;
      info["value2"] = UserSession.CurrentElectionName;
      info["value3"] = message;

      var url = "https://maker.ifttt.com/trigger/{0}/with/key/{1}".FilledWith("TallyJ", iftttKey);

      using (var client = new WebClient())
      {
        var response = client.UploadValues(url, info);
      }
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