using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  public class EmailHelper
  {
    private LogHelper _logHelper;

    public bool SendVoterEmailTest(string email, out string error)
    {
      var hostSite = SettingsHelper.Get("HostSite", "");

      var html = GetEmailTemplate("TestEmail").FilledWithObject(new
      {
        when = DateTime.Today.ToShortDateString(),
        month = DateTime.Today.ToString("MMMM"),
        hostSite,
        logo = hostSite + "/Images/LogoSideM.png",
        email
      });

      var message = new MailMessage();
      message.To.Add(email);
      //      message.ReplyToList.Add(new MailAddress("glen.little@gmail.com", "Glen Little"));


      var ok = SendEmail(message, html, out error);

      LogHelper.Add($"Voter self-test email sent", true);

      return ok;
    }

    //    public bool SendWhenOpen(Election e, Person p, OnlineVotingInfo ovi, OnlineVoter ov, out string error)
    //    {
    //      // only send if they asked for it
    //      if (!ov.EmailCodes.Contains("o"))
    //      {
    //        error = null;
    //        return true;
    //      }
    //
    //      // proceed to send
    //      var email = ov.Email;
    //      var hostSite = SettingsHelper.Get("HostSite", "");
    //
    //      var whenClose = e.OnlineWhenClose.GetValueOrDefault();
    //      var openLength = whenClose - e.OnlineWhenOpen.GetValueOrDefault();
    //      var howLong = "";
    //      if (openLength.Days > 1)
    //      {
    //        howLong = openLength.Days + " days";
    //      }
    //      else
    //      {
    //        howLong = openLength.Hours + " hours";
    //      }
    //
    //      var html = GetEmailTemplate("BallotProcessed").FilledWithObject(new
    //      {
    //        email,
    //        voterName = p.C_FullNameFL,
    //        electionName = e.Name,
    //        electionType = ElectionTypeEnum.TextFor(e.ElectionType),
    //        hostSite,
    //        logo = hostSite + "/Images/LogoSideM.png",
    //        howLong,
    //        whenClose
    //      });
    //
    //      var message = new MailMessage();
    //      message.To.Add(new MailAddress(email, p.C_FullNameFL));
    //
    //      var memberEmail = UserSession.MemberEmail;
    //      if (memberEmail.HasContent())
    //      {
    //        message.ReplyToList.Add(new MailAddress(memberEmail, UserSession.MemberName + " (Teller)"));
    //      }
    //
    //      return SendEmail(message, html, out error);
    //    }

    public bool SendWhenProcessed(Election e, Person p, OnlineVotingInfo ovi, OnlineVoter ov, out string error)
    {
      // only send if they asked for it
      if (!ov.EmailCodes.Contains("p"))
      {
        error = null;
        return true;
      }

      // proceed to send
      var email = ov.Email;
      var hostSite = SettingsHelper.Get("HostSite", "");

      var html = GetEmailTemplate("BallotProcessed").FilledWithObject(new
      {
        email,
        voterName = p.C_FullNameFL,
        electionName = e.Name,
        electionType = ElectionTypeEnum.TextFor(e.ElectionType),
        hostSite,
        logo = hostSite + "/Images/LogoSideM.png",
      });

      var message = new MailMessage();
      message.To.Add(new MailAddress(email, p.C_FullNameFL));

      var fromAddress = e.EmailFromAddress;
      if (fromAddress.HasContent())
      {
        message.From = new MailAddress(fromAddress, e.EmailFromName);
      }

      var ok = SendEmail(message, html, out error);

      // logging done at a higher level

      return ok;
    }

    private LogHelper LogHelper => _logHelper ?? (_logHelper = new LogHelper());

    private JsonResult SendWhenOpened(bool automated = false)
    {
      var db = UserSession.GetNewDbContext;
      var now = DateTime.Now;
      var hostSite = SettingsHelper.Get("HostSite", "");

      var electionList = db.Election
        // elections that are open but have not been announced
        .Where(e => e.OnlineWhenOpen < now
                    && e.OnlineAnnounced == null
                    && e.OnlineWhenClose > now)
        .GroupJoin(
          // get list of voters who want to be notified when their election opens
          db.OnlineVotingInfo
            .Join(db.OnlineVoter.Where(ov => ov.EmailCodes.Contains("o")), ovi => ovi.Email, ov => ov.Email, (ovi, ov) => new { ovi, ov })
            .Join(db.Person, j => j.ovi.PersonGuid, p => p.PersonGuid, (j, p) => new { j.ovi, j.ov, p })
          , e => e.ElectionGuid, jv => jv.ovi.ElectionGuid, (election, oviList) => new { election, oviList })
        .Select(g => new
        {
          g.election,
          people = g.oviList.Select(jOvi => new
          {
            jOvi.ov.Email,
            jOvi.p.C_FullNameFL,
          })
        })
        .ToList();

      var allMsgs = new List<string>();

      electionList.ForEach(electionInfo =>
      {
        var electionName = electionInfo.election.Name;
        var electionType = ElectionTypeEnum.TextFor(electionInfo.election.ElectionType);
        //        var isEstimate = electionInfo.election.OnlineCloseIsEstimate;
        var whenClosed = electionInfo.election.OnlineWhenClose.GetValueOrDefault();
        var whenClosedUtc = whenClosed.ToUniversalTime();
        var remainingTime = whenClosed - now;
        var howLong = "";
        if (remainingTime.Days > 1)
        {
          howLong = remainingTime.Days + " days";
        }
        else
        {
          howLong = remainingTime.Hours + " hours";
        }

        var numEmails = 0;
        var errors = new List<string>();

        electionInfo.people.ToList().ForEach(p =>
        {
          var html = GetEmailTemplate("ElectionOpen").FilledWithObject(new
          {
            hostSite,
            logo = hostSite + "/Images/LogoSideM.png",
            p.Email,
            personName = p.C_FullNameFL,
            electionName,
            electionType,
            whenClosedDay = whenClosedUtc.ToString("d MMM"),
            whenClosedTime = whenClosedUtc.ToString("h:mm tt"),
            howLong,
          });

          var message = new MailMessage();
          message.To.Add(new MailAddress(p.Email, p.C_FullNameFL));

          if (electionInfo.election.EmailFromAddress.HasContent())
          {
            message.From = new MailAddress(electionInfo.election.EmailFromAddress, electionInfo.election.EmailFromName);
          }

          var ok = SendEmail(message, html, out var emailError);

          if (ok)
          {
            numEmails++;
          }
          else
          {
            errors.Add(emailError);
          }
        });

        var msg = $"Email: Election #{electionInfo.election.C_RowId} {(automated ? "automatically " : "")}announced to {numEmails} {(numEmails == 1 ? "person" : "people")}";

        if (errors.Count > 0)
        {
          msg += $" {errors.Count} failed to send.";
        }


        electionInfo.election.OnlineAnnounced = now;
        db.SaveChanges();

        allMsgs.Add(msg);

        // after adding the message to allMsgs, add more details for the log
        if (errors.Count > 0)
        {
          msg += $" First error: {errors[0]}";
        }

        LogHelper.Add(msg, true);

      });

      var numElections = electionList.Count;

      // return the messages for the Azure logs (or any random anonymous caller)

      if (numElections == 0)
      {
        return new
        {
          msg = "Nothing at " + now
        }.AsJsonResult();
      }

      return new
      {
        msg = allMsgs.JoinedAsString("\r\n")
      }.AsJsonResult();
    }

    public JsonResult DoScheduled()
    {
      return SendWhenOpened(true);
    }

    private enum HtEmailCodes
    {
      Test,
      Intro,
      _unknown_
    }

    /// <summary>
    /// requested by the head teller
    /// </summary>
    /// <param name="emailCode">
    ///Expected: test, announce
    /// </param>
    /// <returns></returns>
    public JsonResult SendHeadTellerEmail(string emailCode)
    {
      var htEmailCode = emailCode.AsEnum(HtEmailCodes._unknown_);

      if (htEmailCode == HtEmailCodes._unknown_)
      {
        return new
        {
          Success = false,
          Status = "Invalid request"
        }.AsJsonResult();
      }

      var db = UserSession.GetNewDbContext;
      var now = DateTime.Now;
      var hostSite = SettingsHelper.Get("HostSite", "");

      var election = UserSession.CurrentElection;


      var peopleToSendTo = new List<NameEmail>();
      string emailTemplate;

      switch (htEmailCode)
      {
        case HtEmailCodes.Test:
          emailTemplate = "HeadTellerEmail";
          peopleToSendTo.Add(new NameEmail { Email = election.EmailFromAddress, PersonName = election.EmailFromName });
          break;

        case HtEmailCodes.Intro:
          emailTemplate = "HeadTellerEmail";

          // everyone with an email address
          peopleToSendTo.AddRange(db.Person
            .Where(p => p.ElectionGuid == election.ElectionGuid && p.Email != null && p.Email.Trim().Length > 0)
            .Select(p => new NameEmail
            {
              Email = p.Email,
              PersonName = p.C_FullNameFL,
            })
            );
          break;

        default:
          // not possible
          return null;
      }


      var whenOpen = election.OnlineWhenOpen.GetValueOrDefault();
      var whenOpenUtc = whenOpen.ToUniversalTime();
      var openIsFuture = whenOpen - now > 0.minutes();

      var whenClosed = election.OnlineWhenClose.GetValueOrDefault();
      var whenClosedUtc = whenClosed.ToUniversalTime();
      var remainingTime = whenClosed - now;
      var howLong = "";
      if (remainingTime.Days > 1)
      {
        howLong = remainingTime.Days + " days";
      }
      else
      {
        howLong = remainingTime.Hours + " hours";
      }

      var numEmails = 0;
      var errors = new List<string>();

      peopleToSendTo.ForEach(p =>
      {
        var html = GetEmailTemplate(emailTemplate).FilledWithObject(new
        {
          hostSite,
          logo = hostSite + "/Images/LogoSideM.png",
          p.Email,
          p.PersonName,
          election.EmailText,
          electionName = election.Name,
          electionType = ElectionTypeEnum.TextFor(election.ElectionType),
          openIsFuture,
          whenOpenDay = whenOpenUtc.ToString("d MMM"),
          whenClosedDay = whenClosedUtc.ToString("d MMM"),
          whenClosedTime = whenClosedUtc.ToString("h:mm tt"),
          howLong,
        });

        var message = new MailMessage();
        message.To.Add(new MailAddress(p.Email, p.PersonName));

        if (election.EmailFromAddress.HasContent())
        {
          message.From = new MailAddress(election.EmailFromAddress, election.EmailFromName);
        }

        var ok = SendEmail(message, html, out var emailError);

        if (ok)
        {
          numEmails++;
        }
        else
        {
          errors.Add(emailError);
        }
      });

      var msg = $"Email: Announcement sent to {numEmails} {(numEmails == 1 ? "person" : "people")}";

      if (errors.Count > 0)
      {
        msg += $" {errors.Count} failed to send. First error: {errors[0]}";
      }

      LogHelper.Add(msg, true);

      return new
      {
        Success = numEmails > 0,
        Status = msg
      }.AsJsonResult();
    }

    public JsonResult GetEmailInfo(int lastLogId = 0)
    {
      var dbContext = UserSession.GetNewDbContext;
      var electionGuid = UserSession.CurrentElectionGuid;

      return new
      {
        Success = true,
        NumWithEmails = dbContext.Person
          .Count(p => p.ElectionGuid == electionGuid && p.Email != null && p.Email.Trim().Length > 0),
        Log = dbContext.C_Log
          .Where(l => l.ElectionGuid == electionGuid)
          .Where(l => l.Details.StartsWith("Email:"))
          .Where(l => lastLogId == 0 || l.C_RowId < lastLogId)
          .OrderByDescending(l => l.AsOf)
          .Take(10)
          .Select(l => new
          {
            l.AsOf,
            l.Details,
            l.C_RowId
          })
      }.AsJsonResult();
    }

    public string GetEmailTemplate(string emailTemplate)
    {
      var path = $"{AppDomain.CurrentDomain.BaseDirectory}/EmailTemplates/{emailTemplate}.html";

      AssertAtRuntime.That(File.Exists(path), "Missing email template");

      return File.ReadAllText(path);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="htmlBody"></param>
    /// <param name="errorMessage"></param>
    /// <remarks>
    /// Settings from AppSettings in Web.config or similar:
    /// "FromEmailAddress", "system@tallyj.com"
    /// "SmtpPickupDirectory", "" -- if used, all other settings are ignored
    /// "SmtpHost", "localhost"
    /// "SmtpUsername", ""
    /// "SmtpPassword", ""
    /// "SmtpSecure", false
    /// "SmtpPort", 25
    /// "SmtpTimeoutMs", 5 * 1000
    /// </remarks>
    /// <returns></returns>
    public bool SendEmail(MailMessage message, string htmlBody, out string errorMessage)
    {
      message.Sender = new MailAddress(SettingsHelper.Get("FromEmailAddress", "system@tallyj.com"), "TallyJ System");
      if (message.From == null)
      {
        message.From = message.Sender;
      }
      message.Body = htmlBody;
      message.IsBodyHtml = true;

      var match = Regex.Match(htmlBody, @"<title>(?<subject>.*)</title>");
      var subject = match.Groups["subject"];
      message.Subject = subject.Value;

      var host = SettingsHelper.Get("SmtpHost", "localhost");
      var pickupDirectory = SettingsHelper.Get("SmtpPickupDirectory", "");
      var senderHostName = message.Sender?.Host ?? "TallyJ";

      message.Headers.Add("Message-Id", "<{0}@{1}>".FilledWith(Guid.NewGuid(), senderHostName));

      try
      {
        var smtpUsername = SettingsHelper.Get("SmtpUsername", "");
        var credentials = smtpUsername.HasContent()
          ? new NetworkCredential(smtpUsername, SettingsHelper.Get("SmtpPassword", ""))
          : CredentialCache.DefaultNetworkCredentials;

        using (var smtpClient = new SmtpClient
        {
          Host = host,
          EnableSsl = !pickupDirectory.HasContent() && SettingsHelper.Get("SmtpSecure", false),
          Port = SettingsHelper.Get("SmtpPort", 25),
          PickupDirectoryLocation = pickupDirectory,
          DeliveryMethod = pickupDirectory.HasContent() ? SmtpDeliveryMethod.SpecifiedPickupDirectory : SmtpDeliveryMethod.Network,
          Credentials = credentials,
          Timeout = SettingsHelper.Get("SmtpTimeoutMs", 5 * 1000) // milliseconds
        })
        {
          smtpClient.Send(message);
          // .SendAsync does not help? the entire page is held until the operation finishes or times out
          errorMessage = "";

          return true;
        }
      }
      catch (SmtpFailedRecipientsException ex)
      {
        var who = ex.InnerExceptions.Select(e => e.FailedRecipient).JoinedAsString(", ");
        var why = ex.InnerExceptions.Select(e => e.GetAllMsgs("; "));
        errorMessage = $"Email Failed for {who}: {why}";

        LogHelper.Add(errorMessage, true);
      }
      catch (SmtpFailedRecipientException ex)
      {
        errorMessage = $"Email Failed for {ex.FailedRecipient}: {ex.GetAllMsgs("; ")}";
        LogHelper.Add(errorMessage, true);
      }
      catch (Exception ex)
      {
        errorMessage = ex.GetAllMsgs("; ");
        LogHelper.Add(errorMessage, true);
      }

      return false;
    }
  }

  public class NameEmail
  {
    public string Email { get; set; }
    public string PersonName { get; set; }
  }
}