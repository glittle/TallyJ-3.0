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
    public static bool HostSupportsOnlineElections => ConfigurationManager.AppSettings["HostSupportsOnlineElections"].AsBoolean();

    public static string Get(string name, string defaultValue)
    {
      return ConfigurationManager.AppSettings[name] ?? defaultValue;
    }

    public static bool Get(string name, bool defaultValue)
    {
      var raw = ConfigurationManager.AppSettings[name];
      return raw?.AsBoolean() ?? defaultValue;
    }

    public static int Get(string name, int defaultValue)
    {
      var raw = ConfigurationManager.AppSettings[name];
      return raw?.AsInt() ?? defaultValue;
    }

  }
}
