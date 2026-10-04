using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TallyJ.Code
{
  public static class SettingsHelper
  {
    public static bool HostSupportsOnlineElections => Get("SupportOnlineElections", false);
    public static bool HostSupportsOnlineSmsLogin => Get("SupportOnlineSmsLogin", false);
    public static bool SmsAvailable => Get("SmsAvailable", true) && HostSupportsOnlineSmsLogin; // if Phone is supported, assume sms is available
    public static bool HideSmsCostAppeal => Get("HideSmsCostAppeal", false);
    public static bool VoiceAvailable => Get("VoiceAvailable", true) && HostSupportsOnlineSmsLogin; // if Phone is supported, assume voice is available
    public static int UserAttemptMax => Get("UserAttemptMax", 3); // max code sends in 15 minutes

    /// <summary>Wrong guesses of one voter code before that code is cancelled. Not the send limit.</summary>
    public static int VoterCodeMaxFailedGuesses => AtLeastOne(Get("VoterCodeMaxFailedGuesses", 5));

    /// <summary>Wrong kiosk codes from one IP before that IP must wait. Shared by every kiosk code.</summary>
    public static int KioskCodeMaxFailedGuesses => AtLeastOne(Get("KioskCodeMaxFailedGuesses", 5));
    public static int KioskCodeGuessWindowMinutes => AtLeastOne(Get("KioskCodeGuessWindowMinutes", 15));

    /// <summary>
    /// Wrong kiosk codes from every IP. A wrong code does not identify an election, so this is site-wide.
    /// Kept well above the per-IP limit so one polling place does not trip it.
    /// </summary>
    public static int KioskCodeMaxFailedSitewide => AtLeastOne(Get("KioskCodeMaxFailedSitewide", 200));

    /// <summary>
    /// Comma-separated addresses of reverse proxies allowed to supply X-Forwarded-For.
    /// Empty means the header is ignored and the TCP peer (UserHostAddress) is used.
    /// </summary>
    public static string TrustedProxyIps => Get("TrustedProxyIps", "");

    /// <summary>Wrong guest-teller codes from one IP for one election.</summary>
    public static int TellerJoinMaxFailedPerIp => AtLeastOne(Get("TellerJoinMaxFailedPerIp", 5));
    public static int TellerJoinIpWindowMinutes => AtLeastOne(Get("TellerJoinIpWindowMinutes", 15));

    /// <summary>
    /// Wrong guest-teller codes for one election, from every IP.
    /// Higher than the per-IP limit so one person's typos cannot lock every teller for long.
    /// </summary>
    public static int TellerJoinMaxFailedPerElection => AtLeastOne(Get("TellerJoinMaxFailedPerElection", 30));
    public static int TellerJoinElectionWindowMinutes => AtLeastOne(Get("TellerJoinElectionWindowMinutes", 5));

    private static int AtLeastOne(int value) => value < 1 ? 1 : value;

    public static bool HostSupportsWhatsAppGreenLogin => Get("SupportWhatsAppGreenLogin", false);
    public static bool HostSupportsWhatsAppGreenNotification => Get("SupportWhatsAppGreenNotification", false);
    public static string GreenApiIdInstance => Get("greenapi-IdInstance", "");
    public static string GreenApiTokenInstance => Get("greenapi-ApiTokenInstance", "");
    public static string GreenApiUrl => Get("greenapi-ApiUrl", "https://api.green-api.com");

    public static string Get(string name, string defaultValue)
    {
      return GetRaw(name) ?? defaultValue;
    }

    public static bool Get(string name, bool defaultValue)
    {
      return GetRaw(name)?.AsBoolean() ?? defaultValue;
    }

    public static int Get(string name, int defaultValue)
    {
      return GetRaw(name)?.AsInt() ?? defaultValue;
    }

    /// <summary>
    /// Not using MachineName prefix any more.
    /// Settings specific to a computer should be put into the file referenced in web.config at <appSettings file="my-file.config">...</appSettings>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static string GetRaw(string name)
    {
      //      return ConfigurationManager.AppSettings[$"{Environment.MachineName}:{name}"] ?? ConfigurationManager.AppSettings[name];
      return ConfigurationManager.AppSettings[name];
    }
  }
}
