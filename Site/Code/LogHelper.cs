using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using System.Web;
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
      info["value1"] = "{0} / {1} / {2}".FilledWith(UserSession.LoginId, Environment.MachineName, HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] ?? HttpContext.Current.Request.Url.Host);
      info["value2"] = UserSession.CurrentElectionName;
      info["value3"] = message;

      var url = "https://maker.ifttt.com/trigger/{0}/with/key/{1}".FilledWith("TallyJ", iftttKey);

      using (var client = new WebClientWithTimeout(1000))
      {
        try
        {
          var response = client.UploadValues(url, info);
        }
        catch (Exception) {
          // ignore if we can't send to remote log
        }
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
  public class WebClientWithTimeout : WebClient {
    public WebClientWithTimeout(int timeout) {
      Timeout = timeout;
    }
    public int Timeout { get; set; }

    protected override WebRequest GetWebRequest(Uri address)
    {
      var request = base.GetWebRequest(address);
      request.Timeout = Timeout;
      return request;
    }
  }
}