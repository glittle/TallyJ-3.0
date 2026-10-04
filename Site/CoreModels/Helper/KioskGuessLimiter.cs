using System;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  /// <summary>
  /// Kiosk login sends the secret as the voter id, so a wrong code never finds a row to cancel.
  /// Limit those guesses per IP. A correct code from that IP clears the recent misses.
  /// </summary>
  public class KioskGuessLimiter
  {
    public const string FailurePrefix = "Kiosk sign-in failed ip=";
    public const string SuccessPrefix = "Kiosk sign-in succeeded ip=";
    public const string LockedMessage = "Too many attempts. Please wait before trying again.";

    private readonly ITallyJDbContext _db;
    public int MaxAttempts { get; }
    public int WindowMinutes { get; }

    public KioskGuessLimiter(ITallyJDbContext db, int? maxAttempts = null, int? windowMinutes = null)
    {
      _db = db;
      MaxAttempts = maxAttempts ?? SettingsHelper.KioskCodeMaxFailedGuesses;
      WindowMinutes = windowMinutes ?? SettingsHelper.KioskCodeGuessWindowMinutes;
    }

    public bool IsLocked(string clientIp)
    {
      return FailureCount(clientIp) >= MaxAttempts;
    }

    public AttemptLimitResult RecordFailure(string clientIp)
    {
      var ip = ClientIp.Key(clientIp);
      if (IsLocked(ip))
      {
        return AttemptLimitResult.AlreadyLocked;
      }

      SignInAttemptLog.Write(Guid.Empty, FailurePrefix + ip, false);

      if (!IsLocked(ip))
      {
        return AttemptLimitResult.Recorded;
      }

      SignInAttemptLog.Write(Guid.Empty, "Kiosk sign-in locked after repeated failures ip=" + ip, true);
      return AttemptLimitResult.JustLocked;
    }

    /// <summary>
    /// A right code ends the IP's current miss streak. No log row when there is nothing to clear,
    /// so a normal kiosk login does not add an event.
    /// </summary>
    public void NoteSuccess(string clientIp)
    {
      var ip = ClientIp.Key(clientIp);
      if (FailureCount(ip) == 0)
      {
        return;
      }

      SignInAttemptLog.Write(Guid.Empty, SuccessPrefix + ip, false);
    }

    private int FailureCount(string clientIp)
    {
      var ip = ClientIp.Key(clientIp);
      var windowStart = DateTime.UtcNow.AddMinutes(-WindowMinutes);
      var cutoff = SignInAttemptLog.LatestSince(_db, windowStart, SuccessPrefix + ip, null);
      return SignInAttemptLog.CountSince(_db, cutoff, FailurePrefix + ip, null, null);
    }
  }
}
