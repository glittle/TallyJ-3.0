using System;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  public enum TellerJoinDecision
  {
    Allow,
    UnknownElection,
    InvalidCode,
    Locked
  }

  /// <summary>
  /// Guest teller join limits. One IP is locked after a few wrong codes for an election.
  /// The election itself locks only after many failures in a shorter window, so one person's
  /// typos do not keep every other teller out for long. A right code clears that IP's misses.
  /// </summary>
  public class GuestTellerJoinLimiter
  {
    public const string FailurePrefix = "Guest teller join failed ip=";
    public const string SuccessPrefix = "Guest teller join succeeded ip=";
    public const string InvalidCodeMessage = "Sorry, invalid code entered";
    public const string UnknownElectionMessage = "Sorry, unknown election id";
    public const string LockedMessage = "Too many attempts. Please wait before trying again.";

    private readonly ITallyJDbContext _db;
    public int MaxPerIp { get; }
    public int IpWindowMinutes { get; }
    public int MaxPerElection { get; }
    public int ElectionWindowMinutes { get; }

    public GuestTellerJoinLimiter(
      ITallyJDbContext db,
      int? maxPerIp = null,
      int? ipWindowMinutes = null,
      int? maxPerElection = null,
      int? electionWindowMinutes = null)
    {
      _db = db;
      MaxPerIp = maxPerIp ?? SettingsHelper.TellerJoinMaxFailedPerIp;
      IpWindowMinutes = ipWindowMinutes ?? SettingsHelper.TellerJoinIpWindowMinutes;
      MaxPerElection = maxPerElection ?? SettingsHelper.TellerJoinMaxFailedPerElection;
      ElectionWindowMinutes = electionWindowMinutes ?? SettingsHelper.TellerJoinElectionWindowMinutes;
    }

    public static string GateKeyFor(Guid electionGuid)
    {
      return "tallyj:teller:" + electionGuid.ToString("N");
    }

    public TellerJoinDecision Evaluate(Guid electionGuid, string actualPasscode, string codeToTry, string clientIp)
    {
      // Closed or unknown elections keep today's message and are not counted.
      if (actualPasscode == null)
      {
        return TellerJoinDecision.UnknownElection;
      }

      try
      {
        return AttemptGate.Run(_db, GateKeyFor(electionGuid), () =>
          EvaluateWithinGate(electionGuid, actualPasscode, codeToTry, clientIp));
      }
      catch (AttemptGateDeniedException)
      {
        return TellerJoinDecision.Locked;
      }
    }

    public bool IsLocked(Guid electionGuid, string clientIp)
    {
      try
      {
        return AttemptGate.Run(_db, GateKeyFor(electionGuid), () => IsLockedWithinGate(electionGuid, clientIp));
      }
      catch (AttemptGateDeniedException)
      {
        return true;
      }
    }

    public void NoteSuccess(Guid electionGuid, string clientIp)
    {
      try
      {
        AttemptGate.Run(_db, GateKeyFor(electionGuid), () => NoteSuccessWithinGate(electionGuid, clientIp));
      }
      catch (AttemptGateDeniedException)
      {
        // Leave the streak in place rather than clearing it without the lock.
      }
    }

    private TellerJoinDecision EvaluateWithinGate(Guid electionGuid, string actualPasscode, string codeToTry, string clientIp)
    {
      if (IsLockedWithinGate(electionGuid, clientIp))
      {
        return TellerJoinDecision.Locked;
      }

      if (actualPasscode == codeToTry)
      {
        // Clear this IP before releasing the lock, so a failure waiting behind us cannot
        // lock the IP and then have this success wipe that lock from outside the section.
        NoteSuccessWithinGate(electionGuid, clientIp);
        return TellerJoinDecision.Allow;
      }

      var result = RecordFailureWithinGate(electionGuid, clientIp);
      return result == AttemptLimitResult.Recorded
        ? TellerJoinDecision.InvalidCode
        : TellerJoinDecision.Locked;
    }

    private bool IsLockedWithinGate(Guid electionGuid, string clientIp)
    {
      return IsIpLocked(electionGuid, clientIp) || IsElectionLocked(electionGuid);
    }

    private void NoteSuccessWithinGate(Guid electionGuid, string clientIp)
    {
      var ip = ClientIp.Key(clientIp);
      if (IpFailureCount(electionGuid, ip) == 0)
      {
        return;
      }

      SignInAttemptLog.Write(electionGuid, SuccessPrefix + ip, false);
    }

    private AttemptLimitResult RecordFailureWithinGate(Guid electionGuid, string clientIp)
    {
      var ip = ClientIp.Key(clientIp);
      if (IsLockedWithinGate(electionGuid, ip))
      {
        return AttemptLimitResult.AlreadyLocked;
      }

      SignInAttemptLog.Write(electionGuid, FailurePrefix + ip, false);

      var ipLocked = IsIpLocked(electionGuid, ip);
      var electionLocked = IsElectionLocked(electionGuid);
      if (!ipLocked && !electionLocked)
      {
        return AttemptLimitResult.Recorded;
      }

      // One remote event when the lock starts. Later rejects are not logged.
      var message = electionLocked
        ? "Guest teller join locked for election after repeated failures"
        : "Guest teller join locked after repeated failures ip=" + ip;
      SignInAttemptLog.Write(electionGuid, message, true);
      return AttemptLimitResult.JustLocked;
    }

    private bool IsIpLocked(Guid electionGuid, string clientIp)
    {
      return IpFailureCount(electionGuid, ClientIp.Key(clientIp)) >= MaxPerIp;
    }

    private bool IsElectionLocked(Guid electionGuid)
    {
      var since = DateTime.UtcNow.AddMinutes(-ElectionWindowMinutes);
      var count = SignInAttemptLog.CountSince(_db, since, null, FailurePrefix, electionGuid);
      return count >= MaxPerElection;
    }

    private int IpFailureCount(Guid electionGuid, string ipKey)
    {
      var windowStart = DateTime.UtcNow.AddMinutes(-IpWindowMinutes);
      var cutoff = SignInAttemptLog.LatestSince(_db, windowStart, SuccessPrefix + ipKey, electionGuid);
      return SignInAttemptLog.CountSince(_db, cutoff, FailurePrefix + ipKey, null, electionGuid);
    }
  }
}
