using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class LoginAttemptLimitTests
  {
    private ITallyJDbContext _db;
    private string _ip = "203.0.113.10";
    private Guid _computerGuid;

    [TestInitialize]
    public void Init()
    {
      UnityInstance.Reset();
      _db = new TestDbContext();
      UnityInstance.Offer(_db);
      _ip = "203.0.113.10";
      ClientIp.ResolveForTests = () => _ip;
      UserSession.CurrentContext.Session.Clear();
      _computerGuid = Guid.Empty;
    }

    [TestCleanup]
    public void Cleanup()
    {
      ClientIp.ResolveForTests = null;
      if (_computerGuid != Guid.Empty)
      {
        ComputerCache().TryRemove(_computerGuid, out _);
      }
    }

    [TestMethod]
    public void Defaults_match_the_limits_used_when_app_settings_are_absent()
    {
      SettingsHelper.VoterCodeMaxFailedGuesses.ShouldEqual(5);
      SettingsHelper.KioskCodeMaxFailedGuesses.ShouldEqual(5);
      SettingsHelper.KioskCodeGuessWindowMinutes.ShouldEqual(15);
      SettingsHelper.TellerJoinMaxFailedPerIp.ShouldEqual(5);
      SettingsHelper.TellerJoinIpWindowMinutes.ShouldEqual(15);
      SettingsHelper.TellerJoinMaxFailedPerElection.ShouldEqual(30);
      SettingsHelper.TellerJoinElectionWindowMinutes.ShouldEqual(5);
    }

    [TestMethod]
    public void Voter_code_guesses_keep_other_info_and_cancel_on_the_fifth()
    {
      var voter = new OnlineVoter
      {
        VerifyCode = "123456",
        OtherInfo = "{\"numEl\":2,\"open\":1,\"usedWA\":true}"
      };

      for (var i = 0; i < 4; i++)
      {
        var attempt = VoterCodeAttempts.RegisterWrongGuess(voter);
        attempt.JustCancelled.ShouldEqual(false);
        voter.VerifyCode.ShouldEqual("123456");
      }

      var info = VoterCodeAttempts.Read(voter);
      info.NumElections.ShouldEqual(2);
      info.Open.ShouldEqual(1);
      info.UsedWhatsApp.ShouldEqual(true);
      info.FailedGuesses.ShouldEqual(4);
      Assert.IsTrue(voter.OtherInfo.Length <= VoterCodeAttempts.OtherInfoMaxLength, voter.OtherInfo);

      var cancelled = VoterCodeAttempts.RegisterWrongGuess(voter);
      cancelled.JustCancelled.ShouldEqual(true);
      voter.VerifyCode.ShouldEqual(null);
      VoterCodeAttempts.IsExhausted(voter).ShouldEqual(true);

      VoterCodeAttempts.Reset(voter);
      VoterCodeAttempts.IsExhausted(voter).ShouldEqual(false);
      VoterCodeAttempts.Read(voter).NumElections.ShouldEqual(2);
      VoterCodeAttempts.Read(voter).FailedGuesses.ShouldEqual(0);
    }

    [TestMethod]
    public void Email_code_is_cancelled_after_five_failures_and_a_new_session_does_not_reset_it()
    {
      AddVoter("E", "voter@example.com", "123456");
      UserSession.PendingVoterLogin = "E\tvoter@example.com\temail";
      var helper = new VoterCodeHelper("hub");

      for (var i = 0; i < 4; i++)
      {
        Message(helper.LoginWithCode("000000")).ShouldEqual("Invalid code.");
      }

      var voter = Voter();
      voter.VerifyCode.ShouldEqual("123456");
      voter.VerifyAttempts.ShouldEqual(1);

      // A new browser gets a new session. The pending login is issued again, but the row remembers the misses.
      UserSession.CurrentContext.Session.Clear();
      UserSession.VerifyCodeAttempts = 0;
      UserSession.PendingVoterLogin = "E\tvoter@example.com\temail";

      Message(helper.LoginWithCode("000000")).ShouldEqual(VoterCodeAttempts.CancelledMessage);
      voter.VerifyCode.ShouldEqual(null);

      Message(helper.LoginWithCode("123456")).ShouldEqual(VoterCodeAttempts.CancelledMessage);
      Message(helper.LoginWithCode("000000")).ShouldEqual(VoterCodeAttempts.CancelledMessage);

      CountDetails("Invalid voter signin code").ShouldEqual(4);
      CountDetails("Voter sign-in code cancelled after 5 failed attempts").ShouldEqual(1);
    }

    [TestMethod]
    public void Phone_code_uses_the_same_server_side_counter()
    {
      AddVoter("P", "+15555550100", "654321");
      UserSession.PendingVoterLogin = "P\t+15555550100\tsms";
      var helper = new VoterCodeHelper("hub");

      for (var i = 0; i < 4; i++)
      {
        Message(helper.LoginWithCode("111111")).ShouldEqual("Invalid code.");
      }

      Message(helper.LoginWithCode("000000")).ShouldEqual(VoterCodeAttempts.CancelledMessage);
      Voter().VerifyCode.ShouldEqual(null);
      CountDetails("Voter sign-in code cancelled after 5 failed attempts").ShouldEqual(1);
    }

    [TestMethod]
    public void Email_and_phone_counters_are_independent()
    {
      AddVoter("E", "voter@example.com", "123456");
      AddVoter("P", "+15555550100", "654321");
      var helper = new VoterCodeHelper("hub");

      UserSession.PendingVoterLogin = "E\tvoter@example.com\temail";
      for (var i = 0; i < 4; i++)
      {
        helper.LoginWithCode("000000");
      }

      UserSession.PendingVoterLogin = "P\t+15555550100\tsms";
      Message(helper.LoginWithCode("000000")).ShouldEqual("Invalid code.");

      _db.OnlineVoter.Single(v => v.VoterIdType == "P").VerifyCode.ShouldEqual("654321");
      VoterCodeAttempts.Read(_db.OnlineVoter.Single(v => v.VoterIdType == "E")).FailedGuesses.ShouldEqual(4);
      VoterCodeAttempts.Read(_db.OnlineVoter.Single(v => v.VoterIdType == "P")).FailedGuesses.ShouldEqual(1);
    }

    [TestMethod]
    public void Expired_matching_code_is_not_counted_as_a_guess()
    {
      AddVoter("E", "voter@example.com", "123456", DateTime.UtcNow.AddMinutes(-11));
      UserSession.PendingVoterLogin = "E\tvoter@example.com\temail";

      Message(new VoterCodeHelper("hub").LoginWithCode("123456")).ShouldEqual("Code expired.");

      var voter = Voter();
      voter.VerifyCode.ShouldEqual("123456");
      VoterCodeAttempts.Read(voter).FailedGuesses.ShouldEqual(0);
      CountDetails("Invalid voter signin code").ShouldEqual(0);
    }

    [TestMethod]
    public void Matching_code_before_the_limit_is_not_treated_as_a_failure()
    {
      AddVoter("E", "voter@example.com", "123456");
      UserSession.PendingVoterLogin = "E\tvoter@example.com\temail";
      var helper = new VoterCodeHelper("hub");
      helper.LoginWithCode("000000");
      helper.LoginWithCode("000000");

      Exception thrown = null;
      try
      {
        helper.LoginWithCode("123456");
      }
      catch (Exception ex)
      {
        thrown = ex;
      }

      // Sign-in needs a web request, which these tests do not have. Reaching it means the guess was accepted.
      Assert.IsNotNull(thrown, "A right code should get as far as signing in.");
      Voter().VerifyCode.ShouldEqual("123456");
      VoterCodeAttempts.Read(Voter()).FailedGuesses.ShouldEqual(2);
    }

    [TestMethod]
    public void Missing_pending_login_is_unchanged()
    {
      Message(new VoterCodeHelper("hub").LoginWithCode("123456")).ShouldEqual("Unexpected call");
      Message(new VoterCodeHelper("hub").LoginWithCode(null)).ShouldEqual("Invalid code.");
      _db.C_Log.Count().ShouldEqual(0);
    }

    [TestMethod]
    public void Kiosk_guesses_lock_one_ip_and_leave_another_ip_alone()
    {
      var helper = new VoterCodeHelper("hub");
      _ip = "198.51.100.8";

      for (var i = 0; i < 4; i++)
      {
        Message(helper.LoginWithCode("K_ZZZZ01")).ShouldEqual("Unknown code");
      }

      Message(helper.LoginWithCode("K_ZZZZ02")).ShouldEqual(KioskGuessLimiter.LockedMessage);
      Message(helper.LoginWithCode("K_ZZZZ03")).ShouldEqual(KioskGuessLimiter.LockedMessage);

      CountDetails(KioskGuessLimiter.FailurePrefix).ShouldEqual(5);
      CountContaining("Kiosk sign-in locked").ShouldEqual(1);
      CountDetails("Invalid voter signin code").ShouldEqual(0);

      _ip = "198.51.100.9";
      Message(helper.LoginWithCode("K_ZZZZ04")).ShouldEqual("Unknown code");
      CountDetails(KioskGuessLimiter.FailurePrefix).ShouldEqual(6);
    }

    [TestMethod]
    public void Locked_kiosk_ip_rejects_the_real_code_and_old_misses_expire()
    {
      AddVoter("K", "ABWXYZ", "ABWXYZ");
      var helper = new VoterCodeHelper("hub");
      _ip = "198.51.100.20";

      for (var i = 0; i < 5; i++)
      {
        helper.LoginWithCode("K_MISS0" + (i % 10));
      }

      // K_MISS00 is 8 characters. Five of those locked the IP. Confirm one more time with a real code.
      Message(helper.LoginWithCode("K_ABWXYZ")).ShouldEqual(KioskGuessLimiter.LockedMessage);
      Voter().VerifyCode.ShouldEqual("ABWXYZ");

      foreach (var row in _db.C_Log.ToList())
      {
        row.AsOf = DateTime.UtcNow.AddMinutes(-30);
      }

      Message(helper.LoginWithCode("K_STILL1")).ShouldEqual("Unknown code");
    }

    [TestMethod]
    public void Reissuing_a_kiosk_code_clears_the_guess_count()
    {
      var electionGuid = Guid.NewGuid();
      UserSession.CurrentElectionGuid = electionGuid;
      _db.Person.Add(new Person
      {
        C_RowId = 4,
        ElectionGuid = electionGuid,
        KioskCode = "ABWXYZ",
        FirstName = "Ada",
        LastName = "Lovelace"
      });
      _db.OnlineVoter.Add(new OnlineVoter
      {
        VoterId = "ABWXYZ",
        VoterIdType = "K",
        VerifyCode = "OLD",
        OtherInfo = "{\"numEl\":1,\"open\":1,\"fg\":5}"
      });

      var code = new VoterCodeHelper("hub").GenerateKioskCode(4, out var error);

      error.ShouldEqual("");
      code.ShouldEqual("ABWXYZ");
      var voter = _db.OnlineVoter.Single();
      voter.VerifyCode.ShouldEqual("ABWXYZ");
      VoterCodeAttempts.Read(voter).FailedGuesses.ShouldEqual(0);
      VoterCodeAttempts.Read(voter).NumElections.ShouldEqual(1);
    }

    [TestMethod]
    public void Teller_join_locks_one_ip_and_still_accepts_the_right_code_from_another()
    {
      var electionGuid = OpenElection("secret-code");
      var model = new TellerModel();

      _ip = "203.0.113.50";
      for (var i = 0; i < 4; i++)
      {
        Error(model.GrantAccessToGuestTeller(electionGuid, "wrong", Guid.Empty))
          .ShouldEqual("Sorry, invalid code entered");
      }

      Error(model.GrantAccessToGuestTeller(electionGuid, "wrong", Guid.Empty))
        .ShouldEqual(GuestTellerJoinLimiter.LockedMessage);
      Error(model.GrantAccessToGuestTeller(electionGuid, "secret-code", Guid.Empty))
        .ShouldEqual(GuestTellerJoinLimiter.LockedMessage);

      CountDetails(GuestTellerJoinLimiter.FailurePrefix).ShouldEqual(5);
      CountContaining("Guest teller join locked").ShouldEqual(1);

      _ip = "203.0.113.51";
      Error(model.GrantAccessToGuestTeller(electionGuid, "also-wrong", Guid.Empty))
        .ShouldEqual("Sorry, invalid code entered");

      var limiter = new GuestTellerJoinLimiter(_db);
      limiter.Evaluate(electionGuid, "secret-code", "secret-code", "203.0.113.51")
        .ShouldEqual(TellerJoinDecision.Allow);
      limiter.Evaluate(electionGuid, "secret-code", "secret-code", "203.0.113.50")
        .ShouldEqual(TellerJoinDecision.Locked);
    }

    [TestMethod]
    public void Teller_with_the_right_code_is_not_kept_out_by_their_own_earlier_typos()
    {
      var electionGuid = Guid.NewGuid();
      var limiter = new GuestTellerJoinLimiter(_db, maxPerIp: 5, ipWindowMinutes: 15, maxPerElection: 30, electionWindowMinutes: 5);

      for (var i = 0; i < 4; i++)
      {
        limiter.Evaluate(electionGuid, "secret", "typo", "203.0.113.7")
          .ShouldEqual(TellerJoinDecision.InvalidCode);
      }

      limiter.Evaluate(electionGuid, "secret", "secret", "203.0.113.7")
        .ShouldEqual(TellerJoinDecision.Allow);
      limiter.NoteSuccess(electionGuid, "203.0.113.7");

      for (var i = 0; i < 4; i++)
      {
        limiter.Evaluate(electionGuid, "secret", "typo", "203.0.113.7")
          .ShouldEqual(TellerJoinDecision.InvalidCode);
      }
    }

    [TestMethod]
    public void One_ip_cannot_lock_the_whole_election_and_a_burst_from_many_ips_is_short()
    {
      var electionGuid = Guid.NewGuid();
      var limiter = new GuestTellerJoinLimiter(_db, maxPerIp: 3, ipWindowMinutes: 15, maxPerElection: 6, electionWindowMinutes: 5);

      for (var i = 0; i < 5; i++)
      {
        limiter.Evaluate(electionGuid, "secret", "typo", "203.0.113.1");
      }

      CountDetails(GuestTellerJoinLimiter.FailurePrefix + "203.0.113.1").ShouldEqual(3);
      limiter.Evaluate(electionGuid, "secret", "secret", "203.0.113.2")
        .ShouldEqual(TellerJoinDecision.Allow);

      limiter.Evaluate(electionGuid, "secret", "typo", "203.0.113.2");
      limiter.Evaluate(electionGuid, "secret", "typo", "203.0.113.3");
      limiter.Evaluate(electionGuid, "secret", "typo", "203.0.113.4")
        .ShouldEqual(TellerJoinDecision.Locked);

      limiter.Evaluate(electionGuid, "secret", "secret", "203.0.113.9")
        .ShouldEqual(TellerJoinDecision.Locked);

      var before = _db.C_Log.Count();
      limiter.Evaluate(electionGuid, "secret", "secret", "203.0.113.9");
      _db.C_Log.Count().ShouldEqual(before);

      foreach (var row in _db.C_Log.ToList())
      {
        row.AsOf = DateTime.UtcNow.AddMinutes(-10);
      }

      limiter.Evaluate(electionGuid, "secret", "secret", "203.0.113.9")
        .ShouldEqual(TellerJoinDecision.Allow);
    }

    [TestMethod]
    public void Unknown_election_is_not_counted()
    {
      Error(new TellerModel().GrantAccessToGuestTeller(Guid.NewGuid(), "secret", Guid.Empty))
        .ShouldEqual("Sorry, unknown election id");
      _db.C_Log.Count().ShouldEqual(0);

      GuestTellerJoinLimiter.InvalidCodeMessage.ShouldEqual("Sorry, invalid code entered");
      GuestTellerJoinLimiter.UnknownElectionMessage.ShouldEqual("Sorry, unknown election id");
    }

    [TestMethod]
    public void Client_ip_key_drops_characters_that_do_not_belong_in_a_log_line()
    {
      ClientIp.Key(null).ShouldEqual("unknown");
      ClientIp.Key("  ").ShouldEqual("unknown");
      ClientIp.Key("203.0.113.5").ShouldEqual("203.0.113.5");
      ClientIp.Key("bad ip!\r\n").ShouldEqual("badip");
      ClientIp.ReadFromRequest().ShouldEqual("");
    }

    private void AddVoter(string type, string voterId, string code, DateTime? issuedAt = null)
    {
      _db.OnlineVoter.Add(new OnlineVoter
      {
        VoterId = voterId,
        VoterIdType = type,
        VerifyCode = code,
        VerifyCodeDate = issuedAt ?? DateTime.UtcNow,
        VerifyAttempts = 1
      });
    }

    private OnlineVoter Voter()
    {
      return _db.OnlineVoter.Single();
    }

    private Guid OpenElection(string passcode)
    {
      var electionGuid = Guid.NewGuid();
      _db.Election.Add(new Election
      {
        ElectionGuid = electionGuid,
        Name = "Limit test",
        ListForPublic = true,
        ElectionPasscode = passcode
      });

      _computerGuid = Guid.NewGuid();
      ComputerCache()[_computerGuid] = new Computer
      {
        ComputerGuid = _computerGuid,
        ElectionGuid = electionGuid,
        AuthLevel = "Known",
        LastContact = DateTime.UtcNow,
        AllMyElections = new System.Collections.Generic.List<Guid> { electionGuid }
      };
      return electionGuid;
    }

    private static ConcurrentDictionary<Guid, Computer> ComputerCache()
    {
      var field = typeof(ComputerCacher).GetField("CachedDict", BindingFlags.NonPublic | BindingFlags.Static);
      Assert.IsNotNull(field, "ComputerCacher.CachedDict");
      return (ConcurrentDictionary<Guid, Computer>)field.GetValue(null);
    }

    private int CountDetails(string details)
    {
      return _db.C_Log.Count(l => l.Details == details || (l.Details != null && l.Details.StartsWith(details)));
    }

    private int CountContaining(string text)
    {
      return _db.C_Log.Count(l => l.Details != null && l.Details.Contains(text));
    }

    private static string Message(object result)
    {
      return Prop<string>(result, "Message");
    }

    private static string Error(JsonResult result)
    {
      return Prop<string>(result.Data, "Error");
    }

    private static T Prop<T>(object source, string name)
    {
      var prop = source.GetType().GetProperty(name);
      Assert.IsNotNull(prop, name);
      return (T)prop.GetValue(source);
    }
  }
}
