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
    public static bool HostSupportsOnlineElections => Get("SupportOnlineElections", true);
    public static bool HostSupportsOnlineSmsLogin =>  Get("SupportOnlineSmsLogin", true);

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
