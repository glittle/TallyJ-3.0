using System;
using Newtonsoft.Json;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  /// <summary>
  /// Wrong guesses of a pending voter code, stored on the OnlineVoter row (OtherInfo "fg").
  /// A new browser session cannot reset this. A newly issued code does.
  /// </summary>
  public static class VoterCodeAttempts
  {
    public const string CancelledMessage = "Too many attempts. Please request a new code.";
    public const int OtherInfoMaxLength = 200;

    public static int Max => SettingsHelper.VoterCodeMaxFailedGuesses;

    public static bool IsExhausted(OnlineVoter voter)
    {
      return Read(voter).FailedGuesses >= Max;
    }

    public static OnlineVoterOtherInfo Read(OnlineVoter voter)
    {
      if (voter == null || string.IsNullOrWhiteSpace(voter.OtherInfo))
      {
        return new OnlineVoterOtherInfo();
      }

      try
      {
        return JsonConvert.DeserializeObject<OnlineVoterOtherInfo>(voter.OtherInfo) ?? new OnlineVoterOtherInfo();
      }
      catch (JsonException)
      {
        return new OnlineVoterOtherInfo();
      }
    }

    /// <summary>
    /// Clears the guess count when a replacement code is stored or a login succeeds.
    /// Leaves OtherInfo alone when the count is already zero.
    /// </summary>
    public static void Reset(OnlineVoter voter)
    {
      if (voter == null)
      {
        return;
      }

      var info = Read(voter);
      if (info.FailedGuesses == 0)
      {
        return;
      }

      info.FailedGuesses = 0;
      Write(voter, info);
    }

    public static VoterCodeGuessResult RegisterWrongGuess(OnlineVoter voter)
    {
      var info = Read(voter);
      var max = Max;
      if (info.FailedGuesses >= max)
      {
        voter.VerifyCode = null;
        return new VoterCodeGuessResult
        {
          AlreadyExhausted = true,
          FailedGuesses = info.FailedGuesses
        };
      }

      info.FailedGuesses += 1;
      Write(voter, info);

      var justCancelled = info.FailedGuesses >= max;
      if (justCancelled)
      {
        voter.VerifyCode = null;
      }

      return new VoterCodeGuessResult
      {
        JustCancelled = justCancelled,
        FailedGuesses = info.FailedGuesses
      };
    }

    private static void Write(OnlineVoter voter, OnlineVoterOtherInfo info)
    {
      var json = JsonConvert.SerializeObject(info);
      if (json.Length > OtherInfoMaxLength)
      {
        // The guess counter has to fit. Election counts in this blob are informational.
        json = JsonConvert.SerializeObject(new OnlineVoterOtherInfo { FailedGuesses = info.FailedGuesses });
      }

      voter.OtherInfo = json;
    }
  }

  public class VoterCodeGuessResult
  {
    public bool AlreadyExhausted { get; set; }
    public bool JustCancelled { get; set; }
    public int FailedGuesses { get; set; }
  }
}
