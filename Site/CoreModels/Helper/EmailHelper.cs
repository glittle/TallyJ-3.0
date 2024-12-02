using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  public class EmailHelper : MessageHelperBase
  {

    public bool SendVoterTestMessage(string email, out string error)
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

      // not in an election... using system From email

      var ok = SendEmail(message, html, out error);

      LogHelper.Add($"Email: Voter test message sent", true);

      return ok;
    }

    public bool SendVerifyCodeToVoter(string email, string newCode, out string error)
    {
      var hostSite = SettingsHelper.Get("HostSite", "");

      var html = GetEmailTemplate("VerifyCodeEmail").FilledWithObject(new
      {
        logo = hostSite + "/Images/LogoSideM.png",
        newCode
      });

      var message = new MailMessage();
      message.To.Add(email);

      // not in an election... using system From email

      return SendEmail(message, html, out error);
    }

    public bool SendWhenBallotSubmitted(Person person, Election election, out string error)
    {
      var hostSite = SettingsHelper.Get("HostSite", "");

      var html = GetEmailTemplate("OnSubmit").FilledWithObject(new
      {
        hostSite,
        logo = hostSite + "/Images/LogoSideM.png",
        name = person.C_FullNameFL,
        electionName = election.Name,
        electionType = ElectionTypeEnum.TextFor(election.ElectionType)
      });

      var message = new MailMessage();
      message.To.Add(new MailAddress(person.Email, person.C_FullNameFL));

      var htAddress = new MailAddress(election.EmailFromAddressWithDefault, election.EmailFromNameWithDefault);
      // message.From = htAddress;
      message.ReplyToList.Add(htAddress);

      var ok = SendEmail(message, html, out error);

      LogHelper.Add($"Email: Ballot Submitted", false);

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


    public bool SendWhenProcessed(Election election, Person p, OnlineVoter ov, LogHelper logHelper, out string error)
    {
      // only send if they asked for it
      if (ov.EmailCodes == null || !ov.EmailCodes.Contains("p") || p.Email.HasNoContent())
      {
        error = null;
        return false;
      }


      // proceed to send
      var email = p.Email;

      var hostSite = SettingsHelper.Get("HostSite", "");

      var html = GetEmailTemplate("BallotProcessed").FilledWithObject(new
      {
        email,
        voterName = p.C_FullNameFL,
        electionName = election.Name,
        electionType = ElectionTypeEnum.TextFor(election.ElectionType),
        hostSite,
        logo = hostSite + "/Images/LogoSideM.png",
      });

      var message = new MailMessage();
      message.To.Add(new MailAddress(email, p.C_FullNameFL));

      var htAddress = new MailAddress(election.EmailFromAddressWithDefault, election.EmailFromNameWithDefault);
      // message.From = htAddress;
      // message.Sender = htAddress;
      message.ReplyToList.Add(htAddress);

      var ok = SendEmail(message, html, out error);

      if (ok)
      {
        logHelper.Add("Email: ballot was processed", false, email);
      }

      return ok;
    }

    ///  <summary>
    ///  requested by the head teller
    ///  </summary>
    ///  <param name="list"></param>
    ///  <returns></returns>
    public JsonResult SendHeadTellerEmail(string list)
    {
      // TODO - consider batching and put all emails in BCC.  Limit may be 1000/email. 

      var db = UserSession.GetNewDbContext;
      var hostSite = SettingsHelper.Get("HostSite", "");

      var election = UserSession.CurrentElection;

      var peopleToSendTo = new List<NameEmail>();

      var personIds = list.Replace("[", "").Replace("]", "").Split(',').Select(s => s.AsInt()).ToList();

      peopleToSendTo.AddRange(db.Person
        .Where(p => p.ElectionGuid == election.ElectionGuid && p.Email != null && p.Email.Trim().Length > 0)
        .Where(p => p.CanVoteInElection.Value)
        .Where(p => personIds.Contains(p.C_RowId))
        .Select(p => new NameEmail
        {
          Email = p.Email,
          PersonName = p.C_FullNameFL,
          FirstName = p.FirstName,
          VoterContact = p.Email
        })
        );


      var numEmails = 0;
      var errors = new List<string>();

      peopleToSendTo.ForEach(p =>
      {
        var html = GetEmailTemplate("HeadTellerEmail").FilledWithObject(new
        {
          hostSite,
          logo = hostSite + "/Images/LogoSideM.png",
          p.Email,
          p.PersonName,
          p.FirstName,
          p.VoterContact,
          election.EmailText,
          election.EmailSubject,
          // electionName = election.Name,
          // electionType = ElectionTypeEnum.TextFor(election.ElectionType),
          // openIsFuture,
          // whenOpenDay = whenOpenUtc.ToString("d MMM"),
          // whenClosedDay = whenClosedUtc.ToString("d MMM"),
          // whenClosedTime = whenClosedUtc.ToString("h:mm tt"),
          // howLong,
        });

        var message = new MailMessage();
        try
        {
          message.To.Add(new MailAddress(p.Email, p.PersonName));

          var htAddress = new MailAddress(election.EmailFromAddressWithDefault, election.EmailFromNameWithDefault);
          // message.From = htAddress;
          // message.Sender = htAddress;
          message.ReplyToList.Add(htAddress);

          var ok = SendEmail(message, html, out var emailError);

          if (ok)
          {
            numEmails++;
          }
          else
          {
            errors.Add(emailError);
          }
        }
        catch (Exception e)
        {
          errors.Add(e.Message + $" (email: {p.Email})");
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


    ///  <summary>
    ///  Send notice to new full teller
    ///  </summary>
    ///  <param name="election"></param>
    ///  <param name="email"></param>
    ///  <returns></returns>
    public JsonResult SendFullTellerInvitation(Election election, string email)
    {
      var hostSite = SettingsHelper.Get("HostSite", "");
      var adminAccountEmail = UserSession.AdminAccountEmail;

      var html = GetEmailTemplate("FullTellerInvitation").FilledWithObject(new
      {
        hostSite,
        logo = hostSite + "/Images/LogoSideM.png",
        election.EmailFromName,
        election.Name,
        email,
        adminAccountEmail
      });

      var message = new MailMessage();
      var ok = false;
      string emailError;
      try
      {
        message.To.Add(new MailAddress(email));

        var htAddress = new MailAddress(adminAccountEmail.DefaultTo(election.EmailFromAddressWithDefault));
        message.ReplyToList.Add(htAddress);

        message.CC.Add(htAddress);

        ok = SendEmail(message, html, out emailError);
      }
      catch (Exception e)
      {
        emailError = e.Message;
        LogHelper.Add(e.Message, true);
      }

      return new
      {
        Success = ok,
        Message = emailError
      }.AsJsonResult();
    }



    private string GetEmailTemplate(string emailTemplate)
    {
      var path = $"{AppDomain.CurrentDomain.BaseDirectory}/MessageTemplates/Email/{emailTemplate}.html";

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
      if (message.Sender == null)
      {
        message.Sender = new MailAddress(SettingsHelper.Get("SmtpDefaultFromAddress", "system@tallyj.com"), "TallyJ System");
      }

      if (message.From == null)
      {
        message.From = message.Sender;
      }

      message.Body = htmlBody;
      message.IsBodyHtml = true;

      var match = Regex.Match(htmlBody, @"<title>(?<subject>.*)</title>");
      var subject = match.Groups["subject"];
      message.Subject = subject.Value;


      // use SendGrid API??
      var sendGridApiKey = SettingsHelper.Get("SendGridApiKey", "");
      if (sendGridApiKey.HasContent())
      {
        return SendEmailApi(sendGridApiKey, message, htmlBody, out errorMessage);
      }

      return SendEmailSmtp(message, htmlBody, out errorMessage);
    }

    private bool SendEmailApi(string sendGridApiKey, MailMessage message, string htmlBody, out string errorMessage)
    {
      // Sender is not used by SendGrid
      var msg = new SendGridMessage
      {
        From = message.From.AsSendGridEmailAddress(),
        Subject = message.Subject,
        HtmlContent = htmlBody,
      };
      msg.AddTos(message.To.Select(a => a.AsSendGridEmailAddress()).ToList());

      if (message.CC.Any())
      {
        msg.AddCcs(message.CC.Select(a => a.AsSendGridEmailAddress()).ToList());
      }

      if (message.ReplyToList.Count > 0)
      {
        var replyTo = message.ReplyToList.First().AsSendGridEmailAddress();

        msg.ReplyTo = replyTo;

        if (msg.From.Name != replyTo.Name && replyTo.Name.HasContent())
        {
          msg.From.Name = replyTo.Name + " via " + msg.From.Name;
        }
      }

      var sendGridClient = new SendGridClient(sendGridApiKey);
      var response = sendGridClient.SendEmailAsync(msg).Result;

      if (response.IsSuccessStatusCode)
      {
        errorMessage = "";
        return true;
      }

      errorMessage = $"{response.StatusCode} {response.Body.ReadAsStringAsync()}";
      LogHelper.Add(errorMessage, true);
      return false;
    }

    private bool SendEmailSmtp(MailMessage message, string htmlBody, out string errorMessage)
    {
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

    /// <summary>
    /// A simple tester
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public static bool IsValidEmail(string email)
    {
      var parts = email.Split('@');
      if (parts.Length != 2)
      {
        return false;
      }

      if (parts[0].Length == 0)
      {
        return false;
      }

      var hostParts = parts[1].Split('.');
      if (hostParts.Any(s => s.Length == 0))
      {
        return false;
      }

      if (hostParts.Last().Length == 1)
      {
        return false;
      }

      return true;
    }

    private class NameEmail
    {
      public string Email { get; set; }
      public string PersonName { get; set; }
      public string VoterContact { get; set; }
      public string FirstName { get; set; }
    }

  }
}