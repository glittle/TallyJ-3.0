using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TallyJ.Code;

namespace TallyJ.CoreModels.Helper
{
  /// <summary>
  /// Client address for attempt limits. Defaults to the TCP peer (UserHostAddress).
  /// X-Forwarded-For is used only when that peer is listed in TrustedProxyIps, which is empty
  /// unless a reverse proxy is configured. A client-supplied header cannot rotate the limit key.
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

      return SelectClientAddress(
        request.UserHostAddress,
        request.ServerVariables["HTTP_X_FORWARDED_FOR"],
        SettingsHelper.TrustedProxyIps);
    }

    /// <summary>
    /// Pick the address attempt limits should use.
    /// When the immediate peer is trusted, walk X-Forwarded-For from the right and skip other
    /// trusted proxies, so a client cannot prepend a fake address in front of the one the proxy added.
    /// </summary>
    public static string SelectClientAddress(string userHostAddress, string forwardedFor, string trustedProxyIps)
    {
      var trusted = ParseTrusted(trustedProxyIps);
      var peer = Normalize(userHostAddress);
      if (peer.Length > 0 && trusted.Contains(peer))
      {
        var fromHeader = ClientFromForwarded(forwardedFor, trusted);
        if (fromHeader.Length > 0)
        {
          return fromHeader;
        }
      }

      return peer;
    }

    /// <summary>
    /// Stable log key. Empty or unreadable addresses share one bucket named "unknown".
    /// </summary>
    public static string Key(string ip)
    {
      var cleaned = Normalize(ip);
      if (cleaned.Length == 0)
      {
        return "unknown";
      }

      return cleaned.Length > 80 ? cleaned.Substring(0, 80) : cleaned;
    }

    private static string ClientFromForwarded(string forwardedFor, HashSet<string> trusted)
    {
      if (string.IsNullOrWhiteSpace(forwardedFor))
      {
        return "";
      }

      var hops = forwardedFor.Split(',')
        .Select(Normalize)
        .Where(hop => hop.Length > 0)
        .ToList();

      for (var i = hops.Count - 1; i >= 0; i--)
      {
        if (!trusted.Contains(hops[i]))
        {
          return hops[i];
        }
      }

      return "";
    }

    private static HashSet<string> ParseTrusted(string trustedProxyIps)
    {
      var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrWhiteSpace(trustedProxyIps))
      {
        return set;
      }

      var parts = trustedProxyIps.Split(new[] { ',', ';', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
      foreach (var part in parts)
      {
        var normalized = Normalize(part);
        if (normalized.Length > 0)
        {
          set.Add(normalized);
        }
      }

      return set;
    }

    private static string Normalize(string ip)
    {
      if (string.IsNullOrWhiteSpace(ip))
      {
        return "";
      }

      var text = ip.Trim().Trim('"');
      if (text.StartsWith("[") && text.Contains("]"))
      {
        text = text.Substring(1, text.IndexOf(']') - 1);
      }
      else
      {
        var colon = text.LastIndexOf(':');
        if (colon > 0 && text.IndexOf(':') == colon && text.Count(c => c == '.') == 3)
        {
          text = text.Substring(0, colon);
        }
      }

      var cleaned = new string(text.Trim().Where(c =>
        char.IsLetterOrDigit(c) || c == '.' || c == ':' || c == '%').ToArray());

      return cleaned.ToLowerInvariant();
    }
  }
}
