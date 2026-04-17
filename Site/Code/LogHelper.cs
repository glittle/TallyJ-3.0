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
using TallyJ.Properties;

namespace TallyJ.Code
{
  public interface ILogHelper
  {
    void Add(string message, bool alsoSendToRemoteLog = false, string voterId = null, bool includeLocation = false);
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

    /// <summary>
    /// Add to the log
    /// </summary>
    /// <param name="message"></param>
    /// <param name="alsoSendToRemoteLog"></param>
    /// <param name="voterId"></param>
    /// <param name="includeLocation">Most log entries do not need a location</param>
    public void Add(string message, bool alsoSendToRemoteLog = false, string voterId = null, bool includeLocation = false)
    {
      try
      {
        if (voterId == null)
        {
          if (UserSession.UniqueId.HasContent())
          {
            voterId = UserSession.UniqueId;
          }
        }

        AddToLog(new C_Log
        {
          ElectionGuid = _electionGuid.AsNullableGuid(),
          ComputerCode = UserSession.CurrentComputerCode.DefaultTo(null),
          LocationGuid = includeLocation ? UserSession.CurrentLocationGuid.AsNullableGuid() : null,
          VoterId = voterId,
          Details = message,
          HostAndVersion = HostAndVersion
        });

      }
      catch (Exception e)
      {
        message = message + "\nError in logging: " + e.Message;
        alsoSendToRemoteLog = true;
      }

      if (alsoSendToRemoteLog)
      {
        SendToRemoteLog(message + (voterId.HasContent() ? $" ({voterId})" : ""));
      }
    }

    private string HostAndVersion =>
        $"{Environment.MachineName} / {Settings.Default.VersionNum} / {HttpContext.Current?.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] ?? HttpContext.Current?.Request.Url.Host}";

    public void SendToRemoteLog(string message, bool systemLevel = false, string electionName = null)
    {
      var iftttKey = ConfigurationManager.AppSettings["iftttKey"].DefaultTo("");
      if (iftttKey.HasNoContent())
      {
        return;
      }

      // value 1: machine / version [/ hosturl [/ username]]

      var info = new NameValueCollection();
      if (systemLevel)
      {
        info["value1"] = HostAndVersion;
      }
      else
      {
        info["value1"] = "{0} / {1}".FilledWith(HostAndVersion, UserSession.LoginId);
      }

      if (electionName == null)
      {
        try
        {
          electionName = UserSession.CurrentElectionName;
        }
        catch (Exception)
        {
          if (_electionGuid != Guid.Empty)
          {
            electionName = _electionGuid.ToString();
          }
          else
          {
            electionName = "";
          }
        }
      }

      info["value2"] = electionName;

      info["value3"] = message;

      var url = "https://maker.ifttt.com/trigger/{0}/with/key/{1}".FilledWith("TallyJ", iftttKey);

      using var client = new WebClientWithTimeout(1000);
      try
      {
        client.UploadValues(url, info);
      }
      catch (Exception)
      {
        // ignore if we can't send to remote log
      }
    }

    private void AddToLog(C_Log logItem)
    {
      var db = UserSession.GetNewDbContext;
      logItem.AsOf = DateTime.UtcNow;
      db.C_Log.Add(logItem);
      db.SaveChanges();
    }
  }
  public class WebClientWithTimeout : WebClient
  {
    public WebClientWithTimeout(int timeout)
    {
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