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

      var ok = SendEmail(message, html, out error);

      LogHelper.Add($"Email: Voter test message sent", true);

      return ok;
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
      message.Sender = htAddress;

      var ok = SendEmail(message, html, out error);

      LogHelper.Add($"Email: Vote Submitted", false);

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


    public bool SendWhenProcessed(Election e, Person p, OnlineVoter ov, LogHelper logHelper, out string error)
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
        message.From = new MailAddress(fromAddress, e.EmailFromNameWithDefault);
      }

      var ok = SendEmail(message, html, out error);

      if (ok)
      {
        logHelper.Add("Email: ballot was processed", false, email);
      }

      return ok;
    }
    //
    // public string SendWhenOpened(Election currentElection = null, bool automated = false)
    // {
    //   var db = UserSession.GetNewDbContext;
    //   var now = DateTime.Now;
    //   var hostSite = SettingsHelper.Get("HostSite", "");
    //   var electionGuid = currentElection?.ElectionGuid;
    //   var allElections = currentElection == null;
    //
    //   var electionList = db.Election
    //     // elections that are open but have not been announced
    //     .Where(e => allElections || e.ElectionGuid == electionGuid)
    //     .Where(e => e.OnlineWhenOpen < now
    //                 && e.OnlineAnnounced == null
    //                 && e.OnlineWhenClose > now)
    //     .GroupJoin(
    //       // get list of voters who want to be notified by email when their election opens
    //       db.OnlineVotingInfo
    //         .Where(ovi => !ovi.NotifiedAboutOpening.Value)
    //         .Join(db.Person, ovi => ovi.PersonGuid, p => p.PersonGuid, (ovi, p) => new { ovi, p })
    //         .Join(db.OnlineVoter.Where(ov => ov.EmailCodes.Contains("o")), j => j.p.Email, ov => ov.VoterId, (j, ov) => new { j.ovi, emailOv = ov, j.p })
    //         .Join(db.OnlineVoter.Where(ov => ov.EmailCodes.Contains("o")), j => j.p.Phone, ov => ov.VoterId, (j, ov) => new { j.ovi, phoneOv = ov, j.emailOv, j.p })
    //       , e => e.ElectionGuid, jv => jv.ovi.ElectionGuid, (election, oviList) => new { election, oviList })
    //     .Select(g => new
    //     {
    //       g.election,
    //       peopleEmail = g.oviList.Where(jOvi => jOvi.emailOv != null).Select(jOvi => new
    //       {
    //         jOvi.p.Email,
    //         jOvi.p.C_FullNameFL,
    //         jOvi.ovi
    //       }),
    //       peopleSms = g.oviList.Where(jOvi => jOvi.phoneOv != null).Select(jOvi => new
    //       {
    //         jOvi.p.Phone,
    //         jOvi.p.C_FullNameFL,
    //         jOvi.ovi
    //       })
    //
    //     })
    //     .ToList();
    //
    //   var allMsgs = new List<string>();
    //   var smsHelper = new SmsHelper();
    //
    //   electionList.ForEach(electionInfo =>
    //   {
    //     var election = electionInfo.election;
    //
    //     var electionName = election.Name;
    //     var electionType = ElectionTypeEnum.TextFor(election.ElectionType);
    //     //        var isEstimate = electionInfo.election.OnlineCloseIsEstimate;
    //     var whenClosed = election.OnlineWhenClose.GetValueOrDefault();
    //     var whenClosedUtc = whenClosed.ToUniversalTime();
    //     var remainingTime = whenClosed - now;
    //     var howLong = "";
    //     if (remainingTime.Days > 1)
    //     {
    //       howLong = remainingTime.Days + " days";
    //     }
    //     else
    //     {
    //       howLong = remainingTime.Hours + " hours";
    //     }
    //
    //     var numSent = 0;
    //     var errors = new List<string>();
    //
    //     electionInfo.peopleEmail.ToList().ForEach(voter =>
    //     {
    //       var html = GetEmailTemplate("ElectionOpen").FilledWithObject(new
    //       {
    //         hostSite,
    //         logo = hostSite + "/Images/LogoSideM.png",
    //         voter.Email,
    //         personName = voter.C_FullNameFL,
    //         electionName,
    //         electionType,
    //         whenClosedDay = whenClosedUtc.ToString("d MMM"),
    //         whenClosedTime = whenClosedUtc.ToString("h:mm tt"),
    //         howLong,
    //       });
    //
    //       var message = new MailMessage();
    //       message.To.Add(new MailAddress(voter.Email, voter.C_FullNameFL));
    //
    //       if (election.EmailFromAddress.HasContent())
    //       {
    //         message.From = new MailAddress(election.EmailFromAddressWithDefault, election.EmailFromNameWithDefault);
    //       }
    //
    //       var ok = SendEmail(message, html, out var emailError);
    //
    //       if (ok)
    //       {
    //         numSent++;
    //       }
    //       else
    //       {
    //         errors.Add(emailError);
    //       }
    //
    //       // regardless if the email went or not, record it
    //       voter.ovi.NotifiedAboutOpening = true;
    //     });
    //
    //     electionInfo.peopleSms.ToList().ForEach(voter =>
    //     {
    //       var html = GetEmailTemplate("SmsElectionOpen").FilledWithObject(new
    //       {
    //         hostSite,
    //         voter.Phone,
    //         personName = voter.C_FullNameFL,
    //         electionName,
    //         electionType,
    //         whenClosedDay = whenClosedUtc.ToString("d MMM"),
    //         whenClosedTime = whenClosedUtc.ToString("h:mm tt"),
    //         howLong,
    //       });
    //
    //       var ok = smsHelper.SendWhenOpened(voter.Phone, voter.C_FullNameFL, html, out var smsError);
    //
    //       if (ok)
    //       {
    //         numSent++;
    //       }
    //       else
    //       {
    //         errors.Add(smsError);
    //       }
    //
    //       // regardless if the email went or not, record it
    //       voter.ovi.NotifiedAboutOpening = true;
    //     });
    //
    //     var msg = $"Email: Election #{election.C_RowId} {(automated ? "automatically " : "")}announced to {numSent} {(numSent == 1 ? "person" : "people")}";
    //
    //     if (errors.Count > 0)
    //     {
    //       msg += $" {errors.Count} failed to send.";
    //     }
    //
    //
    //     election.OnlineAnnounced = now;
    //
    //     if (currentElection != null)
    //     {
    //       currentElection.OnlineAnnounced = now;
    //     }
    //
    //     db.SaveChanges();
    //
    //     allMsgs.Add(msg);
    //
    //     // after adding the message to allMsgs, add more details for the log
    //     if (errors.Count > 0)
    //     {
    //       msg += $" First error: {errors[0]}";
    //     }
    //
    //     LogHelper.Add(msg, true);
    //
    //   });
    //
    //   var numElections = electionList.Count;
    //
    //   // return the messages for the Azure logs (or any random anonymous caller)
    //
    //   if (numElections == 0)
    //   {
    //     return "Nothing at " + now;
    //   }
    //
    //   return allMsgs.JoinedAsString("\r\n");
    // }

    // public JsonResult DoScheduled()
    // {
    //   return SendWhenOpened(null, true).AsJsonResult();
    // }

    ///  <summary>
    ///  requested by the head teller
    ///  </summary>
    ///  <param name="emailCode">
    ///    Expected: test, announce
    ///  </param>
    ///  <param name="subject"></param>
    ///  <param name="list"></param>
    ///  <returns></returns>
    public JsonResult SendHeadTellerEmail(string emailCode, string subject, string list)
    {
      // TODO - consider batching and put all emails in BCC.  Limit may be 1000/email. 
      // TODO - send SMS to phone numbers


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

      switch (htEmailCode)
      {
        case HtEmailCodes.Test:
          peopleToSendTo.Add(new NameEmail { Email = election.EmailFromAddressWithDefault, PersonName = election.EmailFromNameWithDefault });
          break;

        case HtEmailCodes.Intro:
          // everyone with an email address
          var personIds = list.Replace("[", "").Replace("]", "").Split(',').Select(s => s.AsInt()).ToList();

          peopleToSendTo.AddRange(db.Person
            .Where(p => p.ElectionGuid == election.ElectionGuid && p.Email != null && p.Email.Trim().Length > 0)
            .Where(p => p.CanVote.Value)
            .Where(p => personIds.Contains(p.C_RowId))
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


      // var whenOpen = election.OnlineWhenOpen.GetValueOrDefault();
      // var whenOpenUtc = whenOpen.ToUniversalTime();
      // var openIsFuture = whenOpen - now > 0.minutes();
      //
      // var whenClosed = election.OnlineWhenClose.GetValueOrDefault();
      // var whenClosedUtc = whenClosed.ToUniversalTime();
      // var remainingTime = whenClosed - now;
      // var howLong = "";
      // if (remainingTime.Days > 1)
      // {
      //   howLong = remainingTime.Days + " days";
      // }
      // else
      // {
      //   howLong = remainingTime.Hours + " hours";
      // }

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
          election.EmailText,
          EmailSubject = subject,
          // electionName = election.Name,
          // electionType = ElectionTypeEnum.TextFor(election.ElectionType),
          // openIsFuture,
          // whenOpenDay = whenOpenUtc.ToString("d MMM"),
          // whenClosedDay = whenClosedUtc.ToString("d MMM"),
          // whenClosedTime = whenClosedUtc.ToString("h:mm tt"),
          // howLong,
        });

        var message = new MailMessage();
        message.To.Add(new MailAddress(p.Email, p.PersonName));

        if (election.EmailFromAddress.HasContent())
        {
          message.From = new MailAddress(election.EmailFromAddress, election.EmailFromNameWithDefault);
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

    private class NameEmail
    {
      public string Email { get; set; }
      public string PersonName { get; set; }
    }
  }
}