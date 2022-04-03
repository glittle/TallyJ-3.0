using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Api.V2010.Account.Call;
using Twilio.Types;
using FeedbackResource = Twilio.Rest.Api.V2010.Account.Message.FeedbackResource;

namespace TallyJ.CoreModels.Helper
{
  public class TwilioHelper : MessageHelperBase
  {
    private static Regex PhoneNumberChecker => new Regex(@"\+[0-9]{4,15}");

    public bool SendVoterSmsTestMessage(string phone, out string error)
    {
      var hostSite = SettingsHelper.Get("HostSite", "");

      var text = GetSmsTemplate("TestSms").FilledWithObject(new
      {
        hostSite,
      });

      // this voter is not in a specific election... just testing from the voter page
      var ok = SendSms(phone, text, null, out error);

      LogHelper.Add($"Sms: Voter test message sent", true);

      return ok;
    }

    public bool SendVerifyCodeToVoter(string phone, string newCode, string method, string hubKey, out string error)
    {
      var text = GetSmsTemplate("VerifyCodeSms").FilledWithObject(new
      {
        newCode
      });

      // this voter is not in a specific election...

      return SendSms(phone, text, null, out error, method);
    }

    public bool SendVerifyCodeToVoterByPhone(string phone, string newCode, string hubKey, out string error)
    {
      var text = GetSmsTemplate("VerifyCodePhone")
        .Replace("*", "      ") // more time
        .FilledWithObject(new
        {
          shortCode = newCode.AddSpaces("; "),
          longCode = newCode.AddSpaces("</Say><Pause></Pause><Say>")
        });

      return SendVoice(phone, text, null, out error);
    }

    public bool SendWhenBallotSubmitted(Person person, Election election, out string error)
    {
      var hostSite = SettingsHelper.Get("HostSite", "");

      var text = GetSmsTemplate("OnSubmit").FilledWithObject(new
      {
        hostSite,
        logo = hostSite + "/Images/LogoSideM.png",
        name = person.C_FullNameFL,
        electionName = election.Name,
        electionType = ElectionTypeEnum.TextFor(election.ElectionType)
      });

      var ok = SendSms(person.Phone, text, person.PersonGuid, out error);

      LogHelper.Add($"Sms: Ballot Submitted", false);

      return ok;
    }

    public bool SendWhenProcessed(Election e, Person p, OnlineVoter ov, LogHelper logHelper, out string error)
    {
      // only send if they asked for it
      if (ov.EmailCodes == null || !ov.EmailCodes.Contains("p") || p.Phone.HasNoContent())
      {
        error = null;
        return false;
      }

      // proceed to send
      var phone = p.Phone;

      var text = GetSmsTemplate("BallotProcessed").FilledWithObject(new
      {
        voterName = p.C_FullNameFL,
        electionName = e.Name,
        electionType = ElectionTypeEnum.TextFor(e.ElectionType),
      });

      var ok = SendSms(phone, text, p.PersonGuid, out error);

      // error logging done at a higher level

      if (ok)
      {
        logHelper.Add("Sms: ballot was processed", false, phone);
      }

      return ok;
    }

    /// <summary>
    ///   requested by the head teller
    /// </summary>
    /// <param name="messageCode">
    ///   Expected: test, announce
    /// </param>
    /// <param name="testPhoneNumber">Used when Testing </param>
    /// <param name="text"></param>
    /// <param name="idList"></param>
    /// <returns></returns>
    public JsonResult SendHeadTellerMessage(string idList)
    {
      // var htMessageCode = messageCode.AsEnum(HtEmailCodes._unknown_);
      //
      // if (htMessageCode == HtEmailCodes._unknown_)
      // {
      //   return new
      //   {
      //     Success = false,
      //     Status = "Invalid request"
      //   }.AsJsonResult();
      // }
      //

      var db = UserSession.GetNewDbContext;
      var hostSite = SettingsHelper.Get("HostSite", "");

      var election = UserSession.CurrentElection;
      var text = election.SmsText;

      if (text.HasNoContent())
      {
        return new
        {
          Success = false,
          Status = "SMS text not set"
        }.AsJsonResult();
      }

      var phoneNumbersToSendTo = new List<NamePhone>();

      // switch (htMessageCode)
      // {
      //   case HtEmailCodes.Test:
      //     phoneNumbersToSendTo.Add(new NamePhone
      //     {
      //       Phone = testPhoneNumber, 
      //       PersonName = election.EmailFromNameWithDefault,
      //       FirstName = "(voter's first name)",
      //       VoterContact = testPhoneNumber
      //     });
      //     break;
      //
      //   case HtEmailCodes.Intro:
      var personIds = idList.Replace("[", "").Replace("]", "").Split(',').Select(s => s.AsInt()).ToList();

      phoneNumbersToSendTo.AddRange(db.Person
        .Where(p => p.ElectionGuid == election.ElectionGuid && p.Phone != null && p.Phone.Trim().Length > 0)
        .Where(p => p.CanVote.Value)
        .Where(p => personIds.Contains(p.C_RowId))
        .Select(p => new NamePhone
        {
          Phone = p.Phone,
          PersonName = p.C_FullNameFL,
          FirstName = p.FirstName,
          VoterContact = p.Phone,
          PersonGuid = p.PersonGuid
        })
      );
      //     break;
      //
      //   default:
      //     // not possible
      //     return null;
      // }

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

      // var numSmsSegments = text.Length / 160;

      var numSent = 0;
      var errors = new List<string>();
      var numToSend = phoneNumbersToSendTo.Count;

      LogHelper.Add($"Sms: Sending to {numToSend} {numToSend.Plural("people", "person")} (see above)", true);
      var startTime = DateTime.Now;

      phoneNumbersToSendTo.ForEach(p =>
      {
        var phoneNumber = p.Phone;

        if (!PhoneNumberChecker.IsMatch(phoneNumber))
        {
          errors.Add("Invalid phone number: " + phoneNumber);
          return;
        }

        var messageText = text.FilledWithObject(new
        {
          hostSite,
          p.PersonName,
          p.FirstName,
          p.VoterContact,
        });

        var ok = SendSms(phoneNumber, messageText, p.PersonGuid, out var errorMessage);

        if (ok)
          numSent++;
        else
          errors.Add(errorMessage);
      });

      var seconds = (DateTime.Now - startTime).TotalSeconds.AsInt();

      var msg2 = $"Sms: Sent to {numSent} {numSent.Plural("people", "person")} in {seconds} second{seconds.Plural()}";
      if (errors.Count > 0) msg2 = $" - {errors.Count} failed to send. First error: {errors[0]}";
      LogHelper.Add(msg2, true);

      return new
      {
        Success = numSent > 0,
        Status = msg2
      }.AsJsonResult();
    }

    public static bool IsValidPhoneNumber(string phoneNumber)
    {
      return PhoneNumberChecker.IsMatch(phoneNumber);
    }

    private string GetSmsTemplate(string emailTemplate)
    {
      var path = $"{AppDomain.CurrentDomain.BaseDirectory}/MessageTemplates/Sms/{emailTemplate}.txt";

      AssertAtRuntime.That(File.Exists(path), "Missing SMS template");

      return File.ReadAllText(path);
    }

    /// <summary>
    /// Send via SMS or WhatsApp
    /// </summary>
    /// <param name="toPhoneNumber"></param>
    /// <param name="messageText"></param>
    /// <param name="personGuid"></param>
    /// <param name="errorMessage"></param>
    /// <param name="method">sms or whatsapp</param>
    /// <returns></returns>
    public bool SendSms(string toPhoneNumber, string messageText, Guid? personGuid, out string errorMessage, string method = "sms")
    {
      var sid = SettingsHelper.Get("twilio-SID", "");
      var token = SettingsHelper.Get("twilio-Token", "");
      var fromNumber = SettingsHelper.Get("twilio-FromNumber", "");
      var messagingSid = SettingsHelper.Get("twilio-MessagingSid", "");
      var callbackUrlRaw = SettingsHelper.Get("twilio-CallbackUrl", "");
      var callbackUrl = callbackUrlRaw.HasContent() ? new Uri(callbackUrlRaw) : null;

      if (sid.HasNoContent() || token.HasNoContent())
      {
        errorMessage = "Server not configured for SMS.";
        return false;
      }

      if (!PhoneNumberChecker.IsMatch(toPhoneNumber))
      {
        errorMessage = "Invalid phone number: " + toPhoneNumber;
        return false;
      }

      if (method == "whatsapp")
      {
        fromNumber = SettingsHelper.Get("twilio-WhatsAppFromNumber", "");
        if (fromNumber.HasNoContent())
        {
          errorMessage = "Server not configured for WhatsApp.";
          return false;
        }

        messagingSid = "";// don't send via SID

        fromNumber = "whatsapp:" + fromNumber;
        toPhoneNumber = "whatsapp:" + toPhoneNumber;
      }


      TwilioClient.Init(sid, token);

      try
      {
        MessageResource messageResource;

        if (messagingSid.HasContent())
        {
          messageResource = MessageResource.Create(
            new PhoneNumber(toPhoneNumber),
            body: messageText,
            messagingServiceSid: messagingSid,
            statusCallback: callbackUrl
          );
        }
        else if (fromNumber.HasContent())
        {
          messageResource = MessageResource.Create(
            new PhoneNumber(toPhoneNumber),
            body: messageText,
            from: new PhoneNumber(fromNumber),
            statusCallback: callbackUrl
          );
        }
        else
        {
          errorMessage = "SMS not configured";
          return false;
        }

        errorMessage = messageResource.ErrorMessage; // null if okay

        UserSession.TwilioMsgId = messageResource.Sid;

        var dbContext = UserSession.GetNewDbContext;
        var utcNow = DateTime.UtcNow;
        dbContext.SmsLog.Add(new SmsLog
        {
          SmsSid = messageResource.Sid,
          Phone = toPhoneNumber,
          SentDate = utcNow,
          ElectionGuid = UserSession.CurrentElectionGuid,
          PersonGuid = personGuid == Guid.Empty ? null : personGuid,
          LastDate = utcNow,
          LastStatus = "submitted"
        });
        dbContext.SaveChanges();

        return errorMessage.HasNoContent();
      }
      catch (ApiException e)
      {
        errorMessage = $"Twilio Error {e.Code} - {e.MoreInfo}";
        return false;
      }
      catch (Exception e)
      {
        errorMessage = e.Message;
        return false;
      }
    }


    /// <summary>
    /// Send via phone
    /// </summary>
    /// <param name="toPhoneNumber"></param>
    /// <param name="messageText">Text. Should include <Say></Say></param>
    /// <param name="personGuid"></param>
    /// <param name="errorMessage"></param>
    /// <param name="method">sms or whatsapp</param>
    /// <returns></returns>
    public bool SendVoice(string toPhoneNumber, string messageText, Guid? personGuid, out string errorMessage)
    {
      var sid = SettingsHelper.Get("twilio-SID", "");
      var token = SettingsHelper.Get("twilio-Token", "");
      var fromNumber = SettingsHelper.Get("twilio-FromNumber", "");

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      if (sid.HasNoContent() || token.HasNoContent())
      {
        errorMessage = "Server not configured for SMS.";
        return false;
      }

      if (!PhoneNumberChecker.IsMatch(toPhoneNumber))
      {
        errorMessage = "Invalid phone number: " + toPhoneNumber;
        return false;
      }

      if (fromNumber.HasNoContent())
      {
        errorMessage = "Server not configured for voice.";
        return false;
      }

      var twiml = $"<Response>{messageText}</Response>";
      // .Replace("<Say>", $"<Say language=\"{cultureHelper.CurrentCultureName}\">");

      TwilioClient.Init(sid, token);

      try
      {
        var callResource = CallResource.Create(
          twiml: new Twiml(twiml),
          to: new PhoneNumber(toPhoneNumber),
          from: new PhoneNumber(fromNumber)
        );

        UserSession.TwilioMsgId = callResource.Sid;

        var dbContext = UserSession.GetNewDbContext;
        var utcNow = DateTime.UtcNow;
        dbContext.SmsLog.Add(new SmsLog
        {
          SmsSid = callResource.Sid,
          Phone = toPhoneNumber,
          SentDate = utcNow,
          ElectionGuid = UserSession.CurrentElectionGuid,
          PersonGuid = personGuid == Guid.Empty ? null : personGuid,
          LastDate = utcNow,
          LastStatus = "voice"
        });
        dbContext.SaveChanges();

        errorMessage = "";
        return true;
      }
      catch (ApiException e)
      {
        if (e.Code > 0)
        {
          errorMessage = DecodeTwilioError(e.Code, e.Message);
        }
        else
        {
          errorMessage = $"Voice not available. ({e.Message})";
        }
        return false;
      }
      catch (Exception e)
      {
        errorMessage = e.GetBaseException().Message;
        if (errorMessage.Contains("Unauthorized"))
        {
          errorMessage = "Configuration error. SMS not available.";
        }
        return false;
      }
    }

    public string CheckVoiceCallStatus()
    {
      var callSid = UserSession.TwilioMsgId;
      if (callSid.HasNoContent())
      {
        return null;
      }

      //https://support.twilio.com/hc/en-us/articles/223132547-What-are-the-Possible-Call-Statuses-and-What-do-They-Mean-

      var callResource = CallResource.Fetch(callSid);
      return callResource.Status.ToString();
    }

    public void SendTwilioConfirmation()
    {
      var messageSid = UserSession.TwilioMsgId;
      if (messageSid.HasContent())
      {
        try
        {
          var sid = SettingsHelper.Get("twilio-SID", "");
          var token = SettingsHelper.Get("twilio-Token", "");
          TwilioClient.Init(sid, token);

          FeedbackResource.Create(pathMessageSid: messageSid, outcome: FeedbackResource.OutcomeEnum.Confirmed);
        }
        catch (Exception)
        {
          // ignore
        }

        UserSession.TwilioMsgId = null;
      }
    }



    private static string DecodeTwilioError(int code, string message)
    {
      switch (code)
      {
        case 21608:
          return "During testing, we can only send to pre-authorized phone numbers. Please contact Glen to get your phone number added.";

        case 63015: // if using whatsapp
          return "You first need to send a message in WhatsApp, as described above.";
      }

      if (message.Contains("Unauthorized"))
      {
        return "Configuration error. SMS not available.";
      }

      return $"Twilio {code}: {message}. SMS not available.";
    }


    public class NamePhone
    {
      public string Phone { get; set; }
      public string PersonName { get; set; }
      public string VoterContact { get; set; }
      public string FirstName { get; set; }
      public Guid PersonGuid { get; set; }
    }

    public void LogSmsStatus(string smsSid, string messageStatus, string to, int? errorCode)
    {
      var dbContext = UserSession.GetNewDbContext;
      var log = dbContext.SmsLog.FirstOrDefault(sl => sl.SmsSid == smsSid);
      if (log == null)
      {
        return;
      }

      log.LastStatus = messageStatus;
      log.ErrorCode = errorCode;
      log.LastDate = DateTime.UtcNow;
      log.Phone = to;

      dbContext.SaveChanges();
    }

    public string GetCallStatus(string sid)
    {

      return CallResource.Fetch(sid)?.Status.ToString();
    }
  }
}