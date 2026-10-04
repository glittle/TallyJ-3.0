using System;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  /// <summary>
  /// Kiosk login sends the secret as the voter id, so a wrong code never finds a row to cancel.
  /// Limit those guesses per IP, plus a higher site-wide cap so rotating addresses cannot
  /// guess without limit. A correct code from an IP clears that IP's misses only.
  /// </summary>
  public class KioskGuessLimiter
  {
    public const string GateKey = "tallyj:kiosk";
    public const string FailurePrefix = "Kiosk sign-in failed ip=";
    public const string SuccessPrefix = "Kiosk sign-in succeeded ip=";
    public const string LockedMessage = "Too many attempts. Please wait before trying again.";

    private readonly ITallyJDbContext _db;
    public int MaxAttempts { get; }
    public int WindowMinutes { get; }
    public int MaxSitewide { get; }

    public KioskGuessLimiter(ITallyJDbContext db, int? maxAttempts = null, int? windowMinutes = null, int? maxSitewide = null)
    {
      _db = db;
      MaxAttempts = maxAttempts ?? SettingsHelper.KioskCodeMaxFailedGuesses;
      WindowMinutes = windowMinutes ?? SettingsHelper.KioskCodeGuessWindowMinutes;
      MaxSitewide = maxSitewide ?? SettingsHelper.KioskCodeMaxFailedSitewide;
    }

    public bool IsLocked(string clientIp)
    {
      try
      {
        return AttemptGate.Run(_db, GateKey, () => IsLockedWithinGate(clientIp));
      }
      catch (AttemptGateDeniedException)
      {
        return true;
      }
    }

    public AttemptLimitResult RecordFailure(string clientIp)
    {
      try
      {
        return AttemptGate.Run(_db, GateKey, () => RecordFailureWithinGate(clientIp));
      }
      catch (AttemptGateDeniedException)
      {
        return AttemptLimitResult.AlreadyLocked;
      }
    }

    /// <summary>
    /// A right code ends the IP's current miss streak. No log row when there is nothing to clear,
    /// so a normal kiosk login does not add an event.
    /// </summary>
    public void NoteSuccess(string clientIp)
    {
      try
      {
        AttemptGate.Run(_db, GateKey, () => NoteSuccessWithinGate(clientIp));
      }
      catch (AttemptGateDeniedException)
      {
        // Leave the streak in place rather than clearing it without the lock.
      }
    }

    private bool IsLockedWithinGate(string clientIp)
    {
      return IsIpLocked(clientIp) || IsSitewideLocked();
    }

    private AttemptLimitResult RecordFailureWithinGate(string clientIp)
    {
      var ip = ClientIp.Key(clientIp);
      if (IsLockedWithinGate(ip))
      {
        return AttemptLimitResult.AlreadyLocked;
      }

      SignInAttemptLog.Write(Guid.Empty, FailurePrefix + ip, false);

      var ipLocked = IsIpLocked(ip);
      var sitewideLocked = IsSitewideLocked();
      if (!ipLocked && !sitewideLocked)
      {
        return AttemptLimitResult.Recorded;
      }

      // One remote event. The site-wide lock is the one that affects every election.
      var message = sitewideLocked
        ? "Kiosk sign-in locked site-wide after repeated failures"
        : "Kiosk sign-in locked after repeated failures ip=" + ip;
      SignInAttemptLog.Write(Guid.Empty, message, true);
      return AttemptLimitResult.JustLocked;
    }

    private void NoteSuccessWithinGate(string clientIp)
    {
      var ip = ClientIp.Key(clientIp);
      if (FailureCount(ip) == 0)
      {
        return;
      }

      SignInAttemptLog.Write(Guid.Empty, SuccessPrefix + ip, false);
    }

    private bool IsIpLocked(string clientIp)
    {
      return FailureCount(clientIp) >= MaxAttempts;
    }

    private bool IsSitewideLocked()
    {
      var since = DateTime.UtcNow.AddMinutes(-WindowMinutes);
      var count = SignInAttemptLog.CountSince(_db, since, null, FailurePrefix, null);
      return count >= MaxSitewide;
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
