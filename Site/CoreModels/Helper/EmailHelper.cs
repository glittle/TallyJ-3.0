using System;
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

    public bool SendWhenOpen(Election e, Person p, OnlineVotingInfo ovi, OnlineVoter ov, out string error)
    {
      // only send if they asked for it
      if (!ov.EmailCodes.Contains("o"))
      {
        error = null;
        return true;
      }

      // proceed to send
      var email = ov.Email;
      var hostSite = SettingsHelper.Get("HostSite", "");

      var whenClose = e.OnlineWhenClose.GetValueOrDefault();
      var openLength = whenClose - e.OnlineWhenOpen.GetValueOrDefault();
      var howLong = "";
      if (openLength.Days > 1)
      {
        howLong = openLength.Days + " days";
      }
      else
      {
        howLong = openLength.Hours + " hours";
      }

      var html = GetEmailTemplate("BallotProcessed").FilledWithObject(new
      {
        email,
        voterName = p.C_FullNameFL,
        electionName = e.Name,
        electionType = ElectionTypeEnum.TextFor(e.ElectionType),
        hostSite,
        logo = hostSite + "/Images/LogoSideM.png",
        howLong,
        whenClose
      });

      var message = new MailMessage();
      message.To.Add(new MailAddress(email, p.C_FullNameFL));

      var memberEmail = UserSession.MemberEmail;
      if (memberEmail.HasContent())
      {
        message.ReplyToList.Add(new MailAddress(memberEmail, UserSession.MemberName + " (Teller)"));
      }

      return SendEmail(message, html, out error);
    }

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="htmlBody"></param>
    /// <param name="errorMessage"></param>
    /// <remarks>
    /// Settings from AppSettings in Web.config or similar:
    /// "SmtpHost", "localhost"
    /// "SmtpPickupDirectory", ""
    /// "SmtpUsername", ""
    /// "SmtpPassword", ""
    /// "SmtpSecure", false
    /// "SmtpPort", 25
    /// "SmtpTimeoutMs", 5 * 1000
    /// </remarks>
    /// <returns></returns>
    private bool SendEmail(MailMessage message, string htmlBody, out string errorMessage)
    {
      message.From = new MailAddress("no-reply@tallyj.com", "TallyJ System");
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

    private static string GetEmailTemplate(string emailTemplate)
    {
      var path = $"{AppDomain.CurrentDomain.BaseDirectory}/App_Data/{emailTemplate}.html";

      AssertAtRuntime.That(File.Exists(path), "Missing email template");

      return File.ReadAllText(path);
    }

    public JsonResult DoScheduled()
    {
      return new
      {
        notImplemented = true
      }.AsJsonResult();
    }
  }
}