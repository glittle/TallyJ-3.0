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
    public static bool HostSupportsOnlineSmsLogin =>  Get("SupportOnlineSmsLogin", false);
    public static bool SmsAvailable =>  Get("SmsAvailable", true); // if Phone is supported, assume sms is available
    public static bool VoiceAvailable =>  Get("VoiceAvailable", true); // if Phone is supported, assume voice is available
    public static int UserAttemptMax =>  Get("UserAttemptMax", 10); // max in 15 minutes
    public static bool HostSupportsOnlineWhatsAppLogin =>  Get("SupportOnlineWhatsAppLogin", false);

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
