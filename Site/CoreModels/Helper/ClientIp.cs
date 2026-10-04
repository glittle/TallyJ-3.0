using System;
using System.Linq;
using System.Web;

namespace TallyJ.CoreModels.Helper
{
  /// <summary>
  /// Client address for attempt limits. Uses the first X-Forwarded-For value when a proxy
  /// sends one, otherwise the request's own address. The same choice is already used on log rows.
  /// </summary>
  public static class ClientIp
  {
    /// <summary>
    /// Tests set this. Production leaves it null so the current request is read.
    /// </summary>
    public static Func<string> ResolveForTests { get; set; }

    public static string Current()
    {
      try
      {
        var raw = ResolveForTests != null ? ResolveForTests() : ReadFromRequest();
        return Key(raw);
      }
      catch (Exception)
      {
        return "unknown";
      }
    }

    public static string ReadFromRequest()
    {
      var request = HttpContext.Current?.Request;
      if (request == null)
      {
        return "";
      }

      var forwarded = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
      if (!string.IsNullOrWhiteSpace(forwarded))
      {
        var first = forwarded.Split(',')[0].Trim();
        if (first.Length > 0)
        {
          return first;
        }
      }

      return request.UserHostAddress ?? "";
    }

    /// <summary>
    /// Stable log key. Empty or unreadable addresses share one bucket named "unknown".
    /// </summary>
    public static string Key(string ip)
    {
      if (string.IsNullOrWhiteSpace(ip))
      {
        return "unknown";
      }

      var cleaned = new string(ip.Trim().Where(c =>
        char.IsLetterOrDigit(c) || c == '.' || c == ':' || c == '%').ToArray());

      if (cleaned.Length == 0)
      {
        return "unknown";
      }

      return cleaned.Length > 80 ? cleaned.Substring(0, 80) : cleaned;
    }
  }
}
