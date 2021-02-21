using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Microsoft.SqlServer.Server;
using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.CoreModels.Helper
{
  public class MessageHelperBase
  {
    private LogHelper _logHelper;

    protected LogHelper LogHelper => _logHelper ?? (_logHelper = new LogHelper());

    protected enum HtEmailCodes
    {
      Test,
      Intro,
      _unknown_
    }


    public JsonResult GetContacts()
    {
      var dbContext = UserSession.GetNewDbContext;
      var electionGuid = UserSession.CurrentElectionGuid;

      return new
      {
        Success = true,
        people = dbContext.Person
          .Where(p => p.ElectionGuid == electionGuid && p.CanVote.Value && (p.Email != null && p.Email.Trim().Length > 0 || p.Phone != null && p.Phone.Trim().Length > 0))
          .GroupJoin(dbContext.OnlineVotingInfo.Where(ovi => ovi.ElectionGuid == electionGuid), p => p.PersonGuid, ovi => ovi.PersonGuid,
              (p, oviList) => new { p, OnlineStatus = oviList.Select(ovi => ovi.Status).FirstOrDefault() })
          .OrderBy(j => j.p.C_FullName)
          .Select(j => new
          {
            j.p.C_RowId,
            j.p.C_FullName,
            j.p.VotingMethod,
            j.p.Email,
            j.p.Phone,
            j.OnlineStatus
          }),
      }.AsJsonResult();
    }

    public JsonResult GetContactLog(int lastLogId = 0)
    {
      var log = GetContactLogInternal(lastLogId);

      return new
      {
        Success = true,
        Log = log
      }.AsJsonResult();
    }

    public FileResult DownloadContactLog()
    {
      var log = GetContactLogInternal(0, 999999);
      var timeOffset = UserSession.TimeOffsetServerAhead;

      var lines = new List<string>();
      lines.Add("When,Name,Phone,Action");
      lines.AddRange(log
        .OrderBy(l => l.When)
        .Select(l =>
      {
        // var hasComma = l.Details.Contains(",");
        return new string[]
        {
          l.When.AddMilliseconds(0 - timeOffset).ToString("yyyy MMM d HH:mm"),
          l.Details, // ? l.Details.Replace("\"", "\"\"").SurroundContentWith("\"", "\"") : l.Details
          l.Name,
          l.Phone.SurroundContentWith("Ph: ", ""), // add Ph: to avoid Excel formatting phone number as a number
        }.JoinedAsString(",", "\"", "\"", false);
      }));

      return new FileContentResult(Encoding.UTF8.GetBytes(lines.JoinedAsString("\n")), "text/csv")
      {
        FileDownloadName = "MessageLog.csv"
      };
    }

    private IEnumerable<ContactLogDto> GetContactLogInternal(int lastLogId, int pageSize = 25)
    {
      var dbContext = UserSession.GetNewDbContext;
      var electionGuid = UserSession.CurrentElectionGuid;

      var logEntries = dbContext.C_Log
        .Where(l => l.ElectionGuid == electionGuid)
        .Where(l => l.Details.StartsWith("Email:") || l.Details.StartsWith("Sms:"))
        .Where(l => lastLogId == 0 || l.C_RowId < lastLogId)
        .OrderByDescending(l => l.AsOf)
        .Take(pageSize)
        .Select(l => new ContactLogDto
        {
          When = l.AsOf,
          Details = l.Details,
          RowId = l.C_RowId
        })
        .ToList();

      var dateOldest = logEntries.Min(l => l.When);
      var dateRecent = DateTime.Now;// logEntries.Max(l => l.When);

      var smsLog = dbContext.SmsLog
        .Where(sms => sms.ElectionGuid == electionGuid)
        .GroupJoin(dbContext.Person.Where(p => p.ElectionGuid == electionGuid), sms => sms.PersonGuid, p => p.PersonGuid, (sms, pList) => new { sms, Name = pList.Select(p=>p.C_FullNameFL).FirstOrDefault() })
        .Where(j => j.sms.SentDate >= dateOldest && j.sms.SentDate <= dateRecent)
        .ToList();

      var twilioErrorCodes = new Dictionary<int, string>
      {
        {30003, "Unreachable - not a mobile phone?"},
        {30005, "Unknown - not a mobile phone?"},
        {30006, "Landline or unreachable carrier"},
        {30008, "Unknown error at Twilio"},
      };

      var fakeId = -1;
      var log = logEntries
        .Concat(smsLog.Select(l =>
        {
          var errMsg = "";
          var code = l.sms.ErrorCode.AsInt();
          if (code > 0)
          {
            errMsg = $" - {l.sms.ErrorCode}{(twilioErrorCodes.ContainsKey(code) ? $" - {twilioErrorCodes[code]}" : "")}";
          }

          return new ContactLogDto
          {
            When = l.sms.LastDate ?? l.sms.SentDate,
            Phone = l.sms.Phone,
            Name = l.Name,
            Details = $"{l.sms.LastStatus}{errMsg}",
            RowId = fakeId--
          };
        }))
        .OrderByDescending(l => l.When);
      return log;
    }

    class ContactLogDto
    {
      public DateTime When { set; get; }
      public string Phone { set; get; }
      public string Name { set; get; }
      public string Details { set; get; }
      public string Action { set; get; }
      public int RowId { set; get; }
    }
  }

}