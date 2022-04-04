using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Resources;
using TallyJ.Code.Session;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  /// <summary>
  ///   Helper for Voter Login.  Generates and sends a code. Once entered, the voter is logged in.
  /// </summary>
  public class VoterCodeHelper
  {
    private const string VerifyCodeSentPrefix = "Verify Code Sent via ";
    private const int UserAttemptMinutes = 15;
    private const int UserAttemptMax = 10;
    private const int EnterCodeWithinMinutes = 10;
    private readonly string _hubKey;
    private readonly VoterCodeHub _voterCodeHub;
    private LogHelper _logHelper;

    public VoterCodeHelper(string hubKey)
    {
      _hubKey = hubKey;
      _voterCodeHub = new VoterCodeHub();
    }

    protected LogHelper LogHelper => _logHelper ?? (_logHelper = new LogHelper());

    /// <summary>
    ///   Make and send the code
    /// </summary>
    /// <param name="type"></param>
    /// <param name="method"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public object IssueCode(string type, string method, string target)
    {
      UserSession.PendingVoterLogin = null;

      var voterIdType = VoterIdTypeEnum.Parse(type);
      if (voterIdType == VoterIdTypeEnum._unknown)
        return new
        {
          Success = false,
          Message = "Unknown type: " + type.CleanedForErrorMessages()
        };

      // validate before we try to use
      var validMessage = "Unknown type";
      if (voterIdType == VoterIdTypeEnum.Email)
        validMessage = EmailHelper.IsValidEmail(target) ? "" : "Invalid email";
      else if (voterIdType == VoterIdTypeEnum.Phone)
        validMessage = TwilioHelper.IsValidPhoneNumber(target) ? "" : "Invalid phone number";

      if (validMessage.HasContent())
        return new
        {
          Success = false,
          Message = validMessage
        };

      _voterCodeHub.SetStatus(_hubKey, "Preparing a code for you...");

      // check throttle limits
      CheckSiteUsageThresholds(out var message);
      if (message.HasContent())
        return new
        {
          Success = false,
          Message = message
        };

      var newCode = MakeAndSaveCode(voterIdType, target, out message);
      if (message.HasContent())
        return new
        {
          Success = false,
          Message = message
        };

      // send code
      var sent = false;
      if (voterIdType == VoterIdTypeEnum.Email)
      {
        sent = SendViaEmail(target, newCode, out message);
        if (message.HasContent())
          _voterCodeHub.SetStatus(_hubKey, "Error: " + message.CleanedForErrorMessages());
        else
          _voterCodeHub.SetStatus(_hubKey, "Your login code has been sent.");

        method = type;
      }
      else if (voterIdType == VoterIdTypeEnum.Phone)
      {
        sent = SendViaTwilio(target, method, newCode, out message);

        if (message.HasContent())
        {
          _voterCodeHub.SetStatus(_hubKey, "Error: " + message.CleanedForErrorMessages());
        }
        else
        {
          if (method != "voice") _voterCodeHub.SetStatus(_hubKey, "Your login code has been sent.");
        }
      }

      if (sent)
      {
        UserSession.PendingVoterLogin = $"{voterIdType}\t{target}\t{method}";

        return new
        {
          Success = true
        };
      }

      return new
      {
        Success = false,
        Message = message
      };
    }


    private string MakeAndSaveCode(VoterIdTypeEnum voterIdType, string target, out string errorMessage)
    {
      var newCode = MakeCode();

      // find or make this OnlineVoter record
      var db = UserSession.GetNewDbContext;
      var onlineVoter = db.OnlineVoter.FirstOrDefault(ov => ov.VoterId == target && ov.VoterIdType == voterIdType);
      var utcNow = DateTime.UtcNow;

      if (onlineVoter == null)
      {
        onlineVoter = new OnlineVoter
        {
          VoterId = target,
          VoterIdType = voterIdType,
          VerifyCode = newCode,
          VerifyCodeDate = utcNow,
          VerifyAttempts = 1,
          VerifyAttemptsStart = utcNow,
          WhenRegistered = utcNow
        };
        db.OnlineVoter.Add(onlineVoter);
      }
      else
      {
        var verifyAttemptsStart = onlineVoter.VerifyAttemptsStart.AsUtc();
        var attempts = onlineVoter.VerifyAttempts.GetValueOrDefault();

        var fromDate = utcNow.AddMinutes(0 - UserAttemptMinutes);

        if (verifyAttemptsStart < fromDate)
        {
          attempts = 0; // reset
          onlineVoter.VerifyAttemptsStart = utcNow;
        }

        if (attempts >= UserAttemptMax)
        {
          errorMessage = "Too many attempts. Please wait before trying again.";
          return null;
        }

        //TODO - ensure not being hit too often

        onlineVoter.VerifyCode = newCode;
        onlineVoter.VerifyCodeDate = utcNow;
        onlineVoter.VerifyAttempts = attempts + 1;
        onlineVoter.VerifyAttemptsStart = utcNow;
      }

      db.SaveChanges();

      errorMessage = "";
      return newCode;
    }

    private void CheckSiteUsageThresholds(out string message)
    {
      // check for excessive system use
      var siteHours = 1;
      var siteMax = 1000;

      var dbContext = UserSession.GetNewDbContext;

      var utcNow = DateTime.UtcNow;
      var fromDate = utcNow.AddHours(0 - siteHours);

      var usageCount = dbContext.C_Log
        .Where(l => l.AsOf > fromDate)
        .Count(l => l.Details.StartsWith(VerifyCodeSentPrefix));

      if (usageCount > siteMax)
      {
        message = "System busy.";
        return;
      }

      // check if this session is too busy
      var attempts = UserSession.VerifyCodeAttempts + 1;
      if (attempts >= UserAttemptMax)
      {
        var attemptsStart = UserSession.VerifyCodeAttemptsStart;

        if (utcNow - attemptsStart < UserAttemptMinutes.minutes())
        {
          message = "Too many attempts.";
          return;
        }

        if (attemptsStart == DateTime.MinValue) UserSession.VerifyCodeAttemptsStart = utcNow;
      }

      UserSession.VerifyCodeAttempts = attempts;

      // // check if this user is too busy
      // var userMinutes = 15;
      // var userMax = 10;
      // fromDate = DateTime.Now.AddMinutes(0 - userMinutes);
      //
      // usageCount = dbContext.C_Log
      //   .Where(l => l.AsOf > fromDate)
      //   .Where(l => l.VoterId == target)
      //   .Count(l => l.Details.StartsWith(VerifyCodeSentPrefix));
      //
      // if (usageCount > userMax)
      // {
      //   message = "Too many attempts. Please wait before trying again.";
      //   return;
      // }

      message = "";
    }

    private bool SendViaTwilio(string phoneNumber, string method, string newCode, out string message)
    {
      UserSession.TwilioMsgId = null;
      var twilioHelper = new TwilioHelper();

      switch (method)
      {
        case "sms":
        case "whatsapp":
          twilioHelper.SendVerifyCodeToVoter(phoneNumber, newCode, method, _hubKey, out message);
          break;

        case "voice":
          twilioHelper.SendVerifyCodeToVoterByPhone(phoneNumber, newCode, _hubKey, out message);
          if (message.HasNoContent()) MonitorCallStatus(twilioHelper);

          break;

        default:
          message = "Unknown method: " + method;
          break;
      }

      return message.HasNoContent();
    }


    private void MonitorCallStatus(TwilioHelper twilioHelper)
    {
      // stay and monitor status
      var sid = UserSession.TwilioMsgId;
      if (sid.HasNoContent()) return;

      var activeStatusList = new[] { "queued", "initiated", "ringing", "in-progress" };

      bool tryAgain;
      do
      {
        var status = twilioHelper.GetCallStatus(sid);
        var statusDisplay = new LangResourceHelper().GetFromList("CallStatus", status) ?? status;

        _voterCodeHub.SetStatus(_hubKey, statusDisplay, status);

        tryAgain = activeStatusList.Contains(status);

        if (tryAgain) Thread.Sleep(1.seconds());
      } while (tryAgain);
    }

    private bool SendViaEmail(string emailAddress, string newCode, out string message)
    {
      var emailHelper = new EmailHelper();
      emailHelper.SendVerifyCodeToVoter(emailAddress, newCode, out message);

      LogHelper.Add(VerifyCodeSentPrefix + "email", true, emailAddress);

      return true; // needed?
    }

    private string MakeCode()
    {
      const int size = 6;
      var min = Math.Pow(10, size - 1).AsInt();
      var max = Math.Pow(10, size).AsInt();
      return new Random().Next(min, max).ToString();
    }

    public object LoginWithCode(string code)
    {
      var parts = UserSession.PendingVoterLogin?.Split('\t');

      if (parts == null || parts.Length != 3)
        return new
        {
          Success = false,
          Message = "Unexpected call"
        };

      var voterIdType = parts[0];
      var voterId = parts[1];
      var method = parts[2];
      var db = UserSession.GetNewDbContext;

      var onlineVoter = db.OnlineVoter.FirstOrDefault(ov => ov.VoterId == voterId && ov.VoterIdType == voterIdType);
      if (onlineVoter == null)
        return new
        {
          Success = false,
          Message = "Unknown target: " + voterId.CleanedForErrorMessages()
        };

      if (onlineVoter.VerifyCode == code)
      {
        // check if it was done in time
        var age = DateTime.UtcNow - onlineVoter.VerifyCodeDate.GetValueOrDefault().AsUtc();
        if (age.TotalMinutes > EnterCodeWithinMinutes)
          // too late
          return new
          {
            Success = false,
            Message = "Code expired."
          };

        // login now!
        var uniqueId = "V:" + voterId;
        var claims = new List<Claim>
        {
          new Claim("UniqueID", uniqueId),
          new Claim("VoterId", voterId),
          new Claim("VoterIdType", voterIdType),
          new Claim("IsVoter", "true")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationType);

        var utcNow = DateTime.UtcNow;
        var authenticationProperties = new AuthenticationProperties
        {
          AllowRefresh = true,
          IsPersistent = false,
          ExpiresUtc = utcNow.AddHours(1)
        };

        HttpContext.Current.GetOwinContext().Authentication.SignIn(authenticationProperties, identity);

        UserSession.VoterLastLogin = onlineVoter.WhenLastLogin.AsUtc() ?? DateTime.MinValue;
        UserSession.VoterLoginSource = method;
        UserSession.PendingVoterLogin = null;

        // update the db
        onlineVoter.WhenLastLogin = utcNow;

        onlineVoter.VerifyCode = null;
        onlineVoter.VerifyAttempts = 0;

        db.SaveChanges();

        var logHelper = new LogHelper();

        logHelper.Add($"Voter login via {method} {voterId}", true);

        new VoterPersonalHub().Login(voterId); // in case same voterId is logged into a different computer

        return new
        {
          Success = true
        };
      }

      LogHelper.Add("Invalid voter signin code", true, voterId);

      return new
      {
        Success = false,
        Message = "Invalid code."
      };
    }
  }
}