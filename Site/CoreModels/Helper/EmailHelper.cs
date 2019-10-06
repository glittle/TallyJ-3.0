using System;
using System.Collections.Generic;
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
    public bool SendTest(string email, out string error)
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
      message.ReplyToList.Add(new MailAddress("glen.little@gmail.com", "Glen Little"));

      return SendEmail(message, html, out error);
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

      var memberEmail = UserSession.MemberEmail;
      if (memberEmail.HasContent())
      {
        message.ReplyToList.Add(new MailAddress(memberEmail, "Teller"));
      }

      return SendEmail(message, html, out error);
    }

    private JsonResult SendWhenOpened()
    {
      var db = UserSession.GetNewDbContext;
      var now = DateTime.Now;
      var hostSite = SettingsHelper.Get("HostSite", "");

      var electionList = db.Election
        .Where(e => e.OnlineWhenOpen < now
                    && e.OnlineAnnounced == null
                    && e.OnlineWhenClose > now)
        .Join(db.JoinElectionUser, e => e.ElectionGuid, jeu => jeu.ElectionGuid, (e, jeu) => new { e, jeu })
        .Join(db.Memberships, j => j.jeu.UserId, m => m.UserId, (j, m) => new { j.e, ownerEmail = m.Email })
        .GroupJoin(
          db.OnlineVotingInfo
            .Join(db.OnlineVoter.Where(ov => ov.EmailCodes.Contains("o")), ovi => ovi.Email, ov => ov.Email, (ovi, ov) => new { ovi, ov })
            .Join(db.Person, j => j.ovi.PersonGuid, p => p.PersonGuid, (j, p) => new { j.ovi, j.ov, p })
          , je => je.e.ElectionGuid, jv => jv.ovi.ElectionGuid, (je, oviList) => new { je, oviList })
        .Select(g => new
        {
          g.je.e,
          g.je.ownerEmail,
          people = g.oviList.Select(jOvi => new
          {
            jOvi.ov.Email,
            jOvi.p.C_FullNameFL,
          })
        })
        .ToList();

      var msgs = new List<string>();

      electionList.ForEach(item =>
      {
        var electionName = item.e.Name;
        var electionType = ElectionTypeEnum.TextFor(item.e.ElectionType);
        var isEstimate = item.e.OnlineCloseIsEstimate;
        var whenClosed = item.e.OnlineWhenClose.GetValueOrDefault();
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

        item.people.ToList().ForEach(p =>
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

          if (item.ownerEmail.HasContent())
          {
            message.ReplyToList.Add(new MailAddress(item.ownerEmail));
          }

          numEmails++;

          var ok = SendEmail(message, html, out var emailError);
        });

        msgs.Add($"Election #{item.e.C_RowId} announced to {numEmails} {(numEmails == 1 ? "person" : "people")}");

        item.e.OnlineAnnounced = now;
        db.SaveChanges();
      });

      var numElections = electionList.Count;

      if (numElections == 0)
      {
        return new
        {
          msg = "Nothing at " + now
        }.AsJsonResult();
      }

      return new
      {
        msg = msgs.JoinedAsString("\r\n")
      }.AsJsonResult();
    }

    public JsonResult DoScheduled()
    {
      return SendWhenOpened();
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
      message.From = new MailAddress(SettingsHelper.Get("FromEmailAddress", "system@tallyj.com"), "TallyJ System");
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

        new LogHelper().Add(errorMessage, true);
      }
      catch (SmtpFailedRecipientException ex)
      {
        errorMessage = $"Email Failed for {ex.FailedRecipient}: {ex.GetAllMsgs("; ")}";
        new LogHelper().Add(errorMessage, true);
      }
      catch (Exception ex)
      {
        errorMessage = ex.GetAllMsgs("; ");
        new LogHelper().Add(errorMessage, true);
      }

      return false;
    }

    public string GetEmailTemplate(string emailTemplate)
    {
      var path = $"{AppDomain.CurrentDomain.BaseDirectory}/EmailTemplates/{emailTemplate}.html";

      AssertAtRuntime.That(File.Exists(path), "Missing email template");

      return File.ReadAllText(path);
    }
  }
}