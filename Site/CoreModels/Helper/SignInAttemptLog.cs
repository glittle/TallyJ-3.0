using System;
using System.Linq;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  public enum AttemptLimitResult
  {
    Recorded,
    JustLocked,
    AlreadyLocked
  }

  /// <summary>
  /// Counts recent sign-in failures from tj._Log. The app login can insert log rows but not
  /// update or delete them, and a log row survives a new session and an app-pool recycle.
  /// Each wrong try is stored locally. IFTTT is notified only when a lockout starts.
  /// </summary>
  static class SignInAttemptLog
  {
    public static void Write(Guid electionGuid, string details, bool alsoSendToRemoteLog)
    {
      var log = electionGuid == Guid.Empty ? new LogHelper() : new LogHelper(electionGuid);
      // Pass a voter id so LogHelper does not read the current principal.
      log.Add(details, alsoSendToRemoteLog, "");
    }

    public static int CountSince(ITallyJDbContext db, DateTime since, string detailsEquals, string detailsPrefix, Guid? electionGuid)
    {
      var query = db.C_Log.Where(l => l.AsOf > since);
      if (electionGuid.HasValue)
      {
        var guid = electionGuid.Value;
        query = query.Where(l => l.ElectionGuid == guid);
      }

      if (detailsEquals != null)
      {
        query = query.Where(l => l.Details == detailsEquals);
      }

      if (detailsPrefix != null)
      {
        query = query.Where(l => l.Details.StartsWith(detailsPrefix));
      }

      return query.Count();
    }

    public static DateTime LatestSince(ITallyJDbContext db, DateTime since, string detailsEquals, Guid? electionGuid)
    {
      var query = db.C_Log.Where(l => l.AsOf > since && l.Details == detailsEquals);
      if (electionGuid.HasValue)
      {
        var guid = electionGuid.Value;
        query = query.Where(l => l.ElectionGuid == guid);
      }

      var times = query.Select(l => l.AsOf).ToList();
      return times.Count == 0 ? since : times.Max();
    }
  }
}
