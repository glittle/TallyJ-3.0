using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace TallyJ.CoreModels.Helper
{
  public class SmsHelper : MessageHelperBase
  {
    private Regex PhoneNumberChecker => new Regex(@"\+[0-9]{4,15}");

    public bool SendVoterTestMessage(string phone, out string error)
    {
      var hostSite = SettingsHelper.Get("HostSite", "");

      var text = GetSmsTemplate("TestEmail").FilledWithObject(new
      {
        hostSite,
      });

      var ok = SendSms(phone, text, out error);

      LogHelper.Add($"Sms: Voter test message sent", true);

      return ok;
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

      var ok = SendSms(person.Phone, text, out error);

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

      var ok = SendSms(phone, text, out error);

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
    /// <param name="list"></param>
    /// <returns></returns>
    public JsonResult SendHeadTellerMessage(string messageCode, string testPhoneNumber, string text, string list)
    {
      var htMessageCode = messageCode.AsEnum(HtEmailCodes._unknown_);

      if (htMessageCode == HtEmailCodes._unknown_)
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

      var phoneNumbersToSendTo = new List<NamePhone>();

      switch (htMessageCode)
      {
        case HtEmailCodes.Test:
          phoneNumbersToSendTo.Add(new NamePhone
          {
            Phone = testPhoneNumber, 
            PersonName = election.EmailFromNameWithDefault,
            FirstName = "(voter's first name)",
            VoterContact = testPhoneNumber
          });
          break;

        case HtEmailCodes.Intro:
          // everyone with an email address
          var personIds = list.Replace("[", "").Replace("]", "").Split(',').Select(s => s.AsInt()).ToList();

          phoneNumbersToSendTo.AddRange(db.Person
            .Where(p => p.ElectionGuid == election.ElectionGuid && p.Phone != null && p.Phone.Trim().Length > 0)
            .Where(p => p.CanVote.Value)
            .Where(p => personIds.Contains(p.C_RowId))
            .Select(p => new NamePhone
            {
              Phone = p.Phone,
              PersonName = p.C_FullNameFL,
              FirstName = p.FirstName,
              VoterContact = p.Phone
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

      // var numSmsSegments = text.Length / 160;

      var numSms = 0;
      var errors = new List<string>();

      phoneNumbersToSendTo.ForEach(p =>
      {
        var phoneNumber = p.Phone;

        if (!PhoneNumberChecker.IsMatch(phoneNumber))
        {
          errors.Add("Invalid phone number: " + phoneNumber);
          return;
        }

        var html = text.FilledWithObject(new
        {
          hostSite,
          p.PersonName,
          EmailText = text,
          p.FirstName,
          p.VoterContact,
          // electionName = election.Name,
          // electionType = ElectionTypeEnum.TextFor(election.ElectionType),
          // openIsFuture,
          // whenOpenDay = whenOpenUtc.ToString("d MMM"),
          // whenClosedDay = whenClosedUtc.ToString("d MMM"),
          // whenClosedTime = whenClosedUtc.ToString("h:mm tt"),
          // howLong,
        });

        var ok = SendSms(phoneNumber, html, out var errorMessage);

        if (ok)
          numSms++;
        else
          errors.Add(errorMessage);
      });

      var msg = $"Sms: Announcement sent to {numSms} {(numSms == 1 ? "person" : "people")}";

      if (errors.Count > 0) msg += $" {errors.Count} failed to send. First error: {errors[0]}";

      LogHelper.Add(msg, true);

      return new
      {
        Success = numSms > 0,
        Status = msg
      }.AsJsonResult();
    }


    private string GetSmsTemplate(string emailTemplate)
    {
      var path = $"{AppDomain.CurrentDomain.BaseDirectory}/MessageTemplates/Sms/{emailTemplate}.txt";

      AssertAtRuntime.That(File.Exists(path), "Missing SMS template");

      return File.ReadAllText(path);
    }

    public bool SendSms(string toPhoneNumber, string messageText, out string errorMessage)
    {
      var sid = SettingsHelper.Get("twilio-SID", "");
      var token = SettingsHelper.Get("twilio-Token", "");
      var fromNumber = SettingsHelper.Get("twilio-FromNumber", "");
      var messagingSid = SettingsHelper.Get("twilio-MessagingSid", "");

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

      TwilioClient.Init(sid, token);

      try
      {
        MessageResource messageResource;

        if (messagingSid.HasContent())
        {
          messageResource = MessageResource.Create(
            new PhoneNumber(toPhoneNumber),
            body: messageText,
            messagingServiceSid: messagingSid
          );
        }
        else if (fromNumber.HasContent())
        {
          messageResource = MessageResource.Create(
            new PhoneNumber(toPhoneNumber),
            body: messageText,
            from: new PhoneNumber(fromNumber)
          );
        }
        else
        {
          errorMessage = "SMS not configured";
          return false;
        }

        errorMessage = messageResource.ErrorMessage; // null if okay

        return errorMessage.HasNoContent();
      }
      catch (Exception e)
      {
        errorMessage = e.Message;
        return false;
      }
    }

    public class NamePhone
    {
      public string Phone { get; set; }
      public string PersonName { get; set; }
      public string VoterContact { get; set; }
      public string FirstName { get; set; }
    }
  }
}