using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Newtonsoft.Json;
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
    private const string GenericResultMsg = "SentIf";
    private const string VerifyCodeSentPrefix = "Verify Code Sent via ";
    private const int UserAttemptMinutes = 15;
    private int UserAttemptMax = SettingsHelper.UserAttemptMax;
    private const int EnterCodeWithinMinutes = 10;
    private readonly string _hubKey;
    private readonly VoterCodeHub _voterCodeHub;
    private LogHelper _logHelper;

    public VoterCodeHelper(string hubKey)
    {
      _hubKey = hubKey;
      _voterCodeHub = new VoterCodeHub();
    }

    protected LogHelper LogHelper => _logHelper ??= new LogHelper();


    private static readonly Random _random = new Random();

    /// <summary>
    ///   Make and send the code
    /// </summary>
    /// <param name="type"></param>
    /// <param name="method"></param>
    /// <param name="target">Email address or phone</param>
    /// <returns></returns>
    public async Task<object> IssueCode(string type, string method, string target)
    {
      UserSession.PendingVoterLogin = null;

      // check throttle limits
      CheckSiteUsageThresholds(out var message);
      if (message.HasContent())
        return new
        {
          Success = false,
          Message = message
        };

      // put artifiical random delay
      var watch = System.Diagnostics.Stopwatch.StartNew();
      const int targetTimeMs = 3000; // The "Floor" duration

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
      {
        validMessage = EmailHelper.IsValidEmail(target) ? "" : "Invalid email";
      }
      else if (voterIdType == VoterIdTypeEnum.Phone)
      {
        if (!SettingsHelper.HostSupportsOnlineSmsLogin && !SettingsHelper.HostSupportsWhatsAppGreenLogin)
        {
          validMessage = "Phone not supported";
        }
        else
        {
          validMessage = TwilioHelper.IsValidPhoneNumber(target) ? "" : "Invalid phone number";
        }
      }
      else if (voterIdType == VoterIdTypeEnum.Kiosk)
      {
        validMessage = "Invalid for kiosk";
      }

      if (validMessage.HasContent())
        return new
        {
          Success = false,
          Message = validMessage
        };

      //_voterCodeHub.SetStatus(_hubKey, "", "Preparing");

      var newCode = MakeCode();

      var openElectionGuid = (Guid?)null;
      var personGuid = (Guid?)null;

      CreateOrUpdateOnlineVoter(voterIdType, target, newCode, out message, ref openElectionGuid, ref personGuid, method: method);

      if (message.HasContent() || openElectionGuid == null || personGuid == null)
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
          _voterCodeHub.SetStatus(_hubKey, GenericResultMsg, GenericResultMsg); // "Error: " + message.CleanedForErrorMessages());
        else
          _voterCodeHub.SetStatus(_hubKey, null, GenericResultMsg); //  "emailSent");

        method = type;
      }
      else if (voterIdType == VoterIdTypeEnum.Phone)
      {
        sent = SendViaPhone(target, method, newCode, openElectionGuid.Value, personGuid.Value, out message);

        if (message.HasContent())
        {
          _voterCodeHub.SetStatus(_hubKey, "Error: " + GenericResultMsg);
          //_voterCodeHub.SetStatus(_hubKey, "Error: " + message.CleanedForErrorMessages());
        }
      }

      // delay so hackers cannot know if it worked or not
      // 2. Calculate how much time is left
      watch.Stop();
      int remainingDelay = targetTimeMs - (int)watch.ElapsedMilliseconds;

      // 3. If we finished faster than the target, wait out the difference
      if (remainingDelay > 0)
      {
        await Task.Delay(remainingDelay);
      }

      if (sent)
      {
        UserSession.PendingVoterLogin = $"{voterIdType}\t{target}\t{method}";

        return new
        {
          //Success = true
          Success = false,
          Message = GenericResultMsg
        };
      }

      return new
      {
        Success = false,
        Message = GenericResultMsg // message
      };
    }

    /// <summary>
    ///  Create or update the OnlineVoter record with the new code
    /// </summary>
    /// <param name="voterIdType"></param>
    /// <param name="voterId"></param>
    /// <param name="newCode"></param>
    /// <param name="errorMessage"></param>
    /// <param name="openElectionGuid">May be passed in (Kiosk) or passed out (Phone or Email)</param>
    /// <param name="personGuid">May be passed in (Kiosk) or passed out (Phone or Email)</param>
    /// <param name="reset">If true, reset the attempt count</param>
    /// <param name="method">Method used for verification (e.g., "whatsapp", "sms", "voice")</param>
    private void CreateOrUpdateOnlineVoter(VoterIdTypeEnum voterIdType, string voterId, string newCode, out string errorMessage, ref Guid? openElectionGuid, ref Guid? personGuid, bool reset = false, string method = null)
    {
      // find or make this OnlineVoter record
      var db = UserSession.GetNewDbContext;
      OnlineVoterOtherInfo electionInfoForDb = null;
      var totalElections = 0;
      var openElectionsCount = 0;

      if (voterIdType == VoterIdTypeEnum.Kiosk)
      {
        // for kiosk, we expect to have the electionGuid and personGuid passed in
        if (openElectionGuid == null || personGuid == null)
        {
          errorMessage = "Invalid kiosk code.";
          return;
        }
      }
      else
      {
        // determine how many elections this voterId is used in, and how many are open
        var electionMatches = voterIdType == VoterIdTypeEnum.Phone
            ? db.Person
                .Where(p => p.Phone == voterId)
                .Join(db.Election, p => p.ElectionGuid, e => e.ElectionGuid, (p, e) => new { e, p.PersonGuid })
                .ToList()
            : voterIdType == VoterIdTypeEnum.Email
                ? db.Person
                    .Where(p => p.Email == voterId)
                    .Join(db.Election, p => p.ElectionGuid, e => e.ElectionGuid, (p, e) => new { e, p.PersonGuid })
                    .ToList()
            : null;

        totalElections = electionMatches?.Select(x => x.e.ElectionGuid).Distinct().Count() ?? 0;

        // get the first of any elections - first if listed for public and not a test, otherwise any
        var openElectionInfos = electionMatches?
          .Where(x => x.e.OnlineCurrentlyOpen)
          .OrderByDescending(x => (x.e.ListForPublic == true ? 3 : 2) + (x.e.ShowAsTest == true ? 0 : 1))
          .Select(x => new { x.e.ElectionGuid, x.PersonGuid })
          .ToList();

        // get the first open election guid
        var firstElectionPersonMatch = openElectionInfos.FirstOrDefault();
        openElectionGuid = firstElectionPersonMatch?.ElectionGuid;
        personGuid = firstElectionPersonMatch?.PersonGuid;

        if (openElectionGuid == Guid.Empty)
        {
          openElectionGuid = null;
        }

        // don't proceed if not in any open elections
        if (openElectionGuid == null)
        {
          //errorMessage = "NoneOpen";
          errorMessage = GenericResultMsg;
          return;
        }

        openElectionsCount = openElectionInfos.Count;
        electionInfoForDb = new OnlineVoterOtherInfo
        {
          NumElections = totalElections,
          Open = openElectionsCount,
          UsedWhatsApp = method == "whatsapp"
        };
      }

      var utcNow = DateTime.UtcNow;
      var onlineVoter = db.OnlineVoter.FirstOrDefault(ov => ov.VoterIdType == voterIdType && ov.VoterId == voterId);
      if (onlineVoter == null)
      {
        onlineVoter = new OnlineVoter
        {
          VoterId = voterId,
          VoterIdType = voterIdType,
          VerifyCode = newCode,
          VerifyCodeDate = utcNow,
          VerifyAttempts = 1,
          VerifyAttemptsStart = utcNow,
          WhenRegistered = utcNow,
          OtherInfo = electionInfoForDb != null ? JsonConvert.SerializeObject(electionInfoForDb) : null
        };
        db.OnlineVoter.Add(onlineVoter);
      }
      else
      {
        // update OtherInfo
        if (voterIdType != VoterIdTypeEnum.Kiosk)
        {
          // use the JSON structure, incase it is extended in the future
          var json = onlineVoter.OtherInfo;
          OnlineVoterOtherInfo otherInfo;
          try
          {
            otherInfo = string.IsNullOrWhiteSpace(json)
                ? new OnlineVoterOtherInfo()
                : JsonConvert.DeserializeObject<OnlineVoterOtherInfo>(json) ?? new OnlineVoterOtherInfo();
          }
          catch (JsonException)
          {
            // Handle invalid JSON gracefully
            otherInfo = new OnlineVoterOtherInfo();
          }

          otherInfo.NumElections = totalElections;
          otherInfo.Open = openElectionsCount;

          if (method == "whatsapp")
          {
            otherInfo.UsedWhatsApp = true;
          }

          onlineVoter.OtherInfo = JsonConvert.SerializeObject(otherInfo);
        }

        var verifyAttemptsStart = onlineVoter.VerifyAttemptsStart.AsUtc();
        var attempts = onlineVoter.VerifyAttempts.GetValueOrDefault();

        var fromDate = utcNow.AddMinutes(0 - UserAttemptMinutes);

        if (verifyAttemptsStart < fromDate || reset)
        {
          attempts = 0; // reset
          onlineVoter.VerifyAttemptsStart = utcNow;
        }

        if (attempts >= UserAttemptMax)
        {
          errorMessage = "Too many attempts. Please wait before trying again.";
          return;
        }

        onlineVoter.VerifyCode = newCode;
        onlineVoter.VerifyCodeDate = utcNow;
        onlineVoter.VerifyAttempts = attempts + 1;
        onlineVoter.VerifyAttemptsStart = utcNow;
      }

      db.SaveChanges();

      errorMessage = "";

      return;
    }

    private void CheckSiteUsageThresholds(out string message)
    {
      var utcNow = DateTime.UtcNow;

      // check if this session is too busy
      var attempts = UserSession.VerifyCodeAttempts + 1;
      if (attempts >= UserAttemptMax)
      {
        var attemptsStart = UserSession.VerifyCodeAttemptsStart;

        if (utcNow - attemptsStart < UserAttemptMinutes.minutes())
        {
          message = "Too many attempts. Please wait before trying again.";
          return;
        }

        if (attemptsStart == DateTime.MinValue) UserSession.VerifyCodeAttemptsStart = utcNow;
      }
      UserSession.VerifyCodeAttempts = attempts;


      // check for excessive system use
      var siteHours = 1;
      var siteMax = 1000;

      var dbContext = UserSession.GetNewDbContext;

      var fromDate = utcNow.AddHours(0 - siteHours);

      var usageCount = dbContext.C_Log
        .Where(l => l.AsOf > fromDate)
        .Count(l => l.Details.StartsWith(VerifyCodeSentPrefix));

      if (usageCount > siteMax)
      {
        message = "System busy. Please try again later.";
        return;
      }



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

    private bool SendViaPhone(string phoneNumber, string method, string newCode, Guid openElectionGuid, Guid personGuid, out string message)
    {
      UserSession.TwilioMsgId = null;

      switch (method)
      {
        case "sms":
          var twilioHelper = new TwilioHelper();
          twilioHelper.SendVerifyCodeToVoter(phoneNumber, newCode, method, _hubKey, openElectionGuid, personGuid, out message);
          if (message.HasNoContent()) MonitorSmsStatus(twilioHelper);
          break;

        case "whatsapp":
          var whatsappHelper = new WhatsAppGreenApiHelper();
          whatsappHelper.SendVerifyCodeToVoter(phoneNumber, newCode, _hubKey, openElectionGuid, personGuid, out message);
          break;

        case "voice":
          var voiceHelper = new TwilioHelper();
          voiceHelper.SendVerifyCodeToVoterByPhone(phoneNumber, newCode, _hubKey, openElectionGuid, personGuid, out message);
          if (message.HasNoContent()) MonitorCallStatus(voiceHelper);
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
        //var statusDisplay = new LangResourceHelper().GetFromList("CallStatus", status) ?? status;

        _voterCodeHub.SetStatus(_hubKey, GenericResultMsg, GenericResultMsg);

        tryAgain = activeStatusList.Contains(status);

        if (tryAgain) Thread.Sleep(1.seconds());
      } while (tryAgain);
    }

    private void MonitorSmsStatus(TwilioHelper twilioHelper)
    {
      // stay and monitor status
      var sid = UserSession.TwilioMsgId;
      if (sid.HasNoContent()) return;

      var activeStatusList = new[] { "accepted", "queued", "sending" };
      // final: delivered, delivery_unknown, undelivered, failed

      bool tryAgain;
      do
      {
        var status = twilioHelper.GetSmsStatus(sid);
        //var statusDisplay = new LangResourceHelper().GetFromList("SmsStatus", status) ?? status;

        _voterCodeHub.SetStatus(_hubKey, GenericResultMsg, GenericResultMsg);

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
      var bytes = new byte[4];
      using (var rng = RandomNumberGenerator.Create())
      {
        rng.GetBytes(bytes);
      }
      // Use modulo to stay within the 6-digit range (100,000 to 999,999)
      var value = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 900000 + 100000;
      return value.ToString();
    }

    public object LoginWithCode(string code)
    {
      string[] parts;

      if (code.StartsWith("K_") && code.Length == 8)
      {
        parts = new[] { "K", code.Substring(2).ToUpper(), "kiosk code" };
        code = parts[1];
      }
      else
      {
        parts = UserSession.PendingVoterLogin?.Split('\t');
      }

      if (parts == null || parts.Length != 3)
      {
        return new
        {
          Success = false,
          Message = "Unexpected call"
        };
      }

      var voterIdType = parts[0];
      var voterId = parts[1];
      var method = parts[2];

      var db = UserSession.GetNewDbContext;

      var onlineVoter = db.OnlineVoter.FirstOrDefault(ov => ov.VoterId == voterId && ov.VoterIdType == voterIdType);
      if (onlineVoter == null)
        return new
        {
          Success = false,
          Message = "Unknown code" // + voterId.CleanedForErrorMessages()
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
          new("UniqueID", uniqueId),
          new("VoterId", voterId),
          new("VoterIdType", voterIdType),
          new("IsVoter", "true")
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

    public string GenerateKioskCode(int personId, out string errorMessage)
    {
      var dbContext = UserSession.GetNewDbContext;
      var electionGuid = UserSession.CurrentElectionGuid;

      // var person = new PersonCacher(dbContext).AllForThisElection.SingleOrDefault(p => p.C_RowId == personId);
      var person = dbContext.Person
        .SingleOrDefault(p => p.C_RowId == personId && p.ElectionGuid == electionGuid);
      if (person == null)
      {
        errorMessage = "Unknown person";
        return null;
      }

      if (person.VotingMethod.HasContent())
      {
        errorMessage = "Already voted";
        return null;
      }

      var kioskCode = person.KioskCode;

      if (kioskCode == null)
      {
        // make a 4 character random letter code (upper case) avoiding I, O, and Q
        var randomCode = "";
        var random = new Random();
        var letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        for (var i = 0; i < 4; i++)
        {
          var index = random.Next(0, letters.Length);
          randomCode += letters[index];
        }

        // get the first letter of the first and last name. If missing, use A
        var firstInitial = (person.FirstName.Substring(0, 1) + "A").Substring(0, 1);
        var lastInitial = (person.LastName.Substring(0, 1) + "A").Substring(0, 1);

        kioskCode = $"{firstInitial}{lastInitial}{randomCode}".ToUpper();
      }

      Guid? electionGuidRef = electionGuid;
      Guid? personGuid = person.PersonGuid;

      // for kiosk voters, the voterId is the kioskCode and also the 'secret' code
      CreateOrUpdateOnlineVoter(VoterIdTypeEnum.Kiosk, kioskCode, kioskCode, out errorMessage, ref electionGuidRef, ref personGuid);

      if (errorMessage.HasContent())
      {
        return null;
      }

      person.KioskCode = kioskCode;


      dbContext.SaveChanges();

      return kioskCode;
    }
  }
}