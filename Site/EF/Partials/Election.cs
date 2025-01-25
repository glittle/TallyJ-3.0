using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using static System.Configuration.ConfigurationManager;

namespace TallyJ.EF;

public enum BallotProcessEnum
{
  // define the supported processes
  Unknown, // not defined yet
  None, // do not use any Gathering Ballots
  Roll, // roll call

  // RegV, // register, vote, collect after
  RegC // register, collect together
}

public enum OnlineSelectionProcessEnum
{
  // define the supported processes - first letter is used in database
  List, // voters choose from list
  Random, // voters type random names
  Both // voters choose from list but can add random names
}

public enum EnvNumModeEnum
{
  // define the supported processes
  None, // do not show any
  Absentee, // only for absentee
  All // for all
}

[Serializable]
public partial class Election : IIndexedForCaching
{
  private const char FlagChar = '~';
  private const char SplitChar = ';';
  private Dictionary<ExtraSettingKey, string> _extraDict;

  public string EmailFromAddressWithDefault =>
    EmailFromAddress ?? SettingsHelper.Get("SmtpDefaultFromAddress", "noreply@tallyj.com");

  public string EmailFromNameWithDefault => EmailFromName ?? SettingsHelper.Get("SmtpDefaultFromName", "TallyJ System");


  /// <summary>
  ///   This is a "fake" column that is embedded into the OwnerLoginId column
  /// </summary>
  /// <remarks>
  ///   Must be a string to serialize out to client
  /// </remarks>
  public string BallotProcessRaw
  {
    get => GetExtraSetting(ExtraSettingKey.BP) ?? BallotProcessEnum.Roll.ToString();
    set
    {
      if (value != null && !Enum.IsDefined(typeof(BallotProcessEnum), value))
      {
        throw new ApplicationException("Invalid process key: " + value);
      }

      SetExtraSetting(ExtraSettingKey.BP, value);
    }
  }


  public BallotProcessEnum BallotProcess
  {
    get
    {
      var bp = BallotProcessRaw;
      if (Enum.IsDefined(typeof(BallotProcessEnum), bp))
        return (BallotProcessEnum)Enum.Parse(typeof(BallotProcessEnum), bp);

      return BallotProcessEnum.Unknown;
    }
  }

  /// <summary>
  ///   This is a "fake" column that is embedded into the OwnerLoginId column
  /// </summary>
  /// <remarks>
  ///   Must be a string to serialize out to client
  /// </remarks>
  public string EnvNumModeRaw
  {
    get => GetExtraSetting(ExtraSettingKey.Env) ?? EnvNumModeEnum.Absentee.ToString();
    set
    {
      if (value != null && !Enum.IsDefined(typeof(EnvNumModeEnum), value))
        throw new ApplicationException("Invalid envelope number mode: " + value);

      SetExtraSetting(ExtraSettingKey.Env, value);
    }
  }

  public EnvNumModeEnum EnvNumMode
  {
    get
    {
      var env = EnvNumModeRaw;
      if (Enum.IsDefined(typeof(EnvNumModeEnum), env)) return (EnvNumModeEnum)Enum.Parse(typeof(EnvNumModeEnum), env);

      return EnvNumModeEnum.Absentee;
    }
  }

  public string Custom1Name => (CustomMethods.DefaultTo("") + "||").Split('|')[0];
  public string Custom2Name => (CustomMethods.DefaultTo("") + "||").Split('|')[1];
  public string Custom3Name => (CustomMethods.DefaultTo("") + "||").Split('|')[2];

  public List<string> FlagsList => Flags.DefaultTo("").SplitWithString("|").ToList();
  //public List<VotingMethodEnum> VotingMethods
  //{
  //  get
  //  {
  //    var list = VotingMethodsRaw.DefaultTo("").Split(',');
  //    return list.Select(s =>
  //    {
  //      if (Enum.IsDefined(typeof(VotingMethodEnum), s))
  //      {
  //        return (VotingMethodEnum) Enum.Parse(typeof(VotingMethodEnum), s);
  //      }

  //      return VotingMethodEnum.Unknown;
  //    }).ToList();
  //  }
  //}


  /// <summary>
  ///   This is a "fake" column that is embedded into the OwnerLoginId column
  /// </summary>
  /// <remarks>
  ///   Must be a string to serialize out to client
  /// </remarks>
  public bool T24
  {
    get => GetExtraSetting(ExtraSettingKey.T24).AsBoolean();
    set => SetExtraSetting(ExtraSettingKey.T24, value ? "1" : "0");
  }

  /// <summary>
  ///   Should the voters list be shown in random order?
  /// </summary>
  /// <remarks>
  ///   Defaults to true for backwards compatibility
  ///   This is a "fake" column.
  /// </remarks>
  public bool RandomizeVotersList
  {
    get => GetExtraSetting(ExtraSettingKey.RV).AsBoolean(true);
    set => SetExtraSetting(ExtraSettingKey.RV, value ? "1" : "0");
  }

  public bool GuestTellersCanAddPeople
  {
    get => GetExtraSetting(ExtraSettingKey.GA).AsBoolean(false);
    set => SetExtraSetting(ExtraSettingKey.GA, value ? "1" : "0");
  }

  public bool IsSingleNameElection =>
    NumberToElect.GetValueOrDefault(0) == 1 && NumberExtra.GetValueOrDefault(0) == 0;


  public bool OnlineEnabled => OnlineWhenOpen.HasValue;

  public bool OnlineCurrentlyOpen
  {
    get
    {
      var utcNow = DateTime.UtcNow;
      return OnlineWhenOpen.HasValue
             && OnlineWhenClose.HasValue
             && OnlineWhenOpen.Value.AsUtc() < utcNow
             && OnlineWhenOpen.Value < OnlineWhenClose.Value // don't need UTC on this line
             && OnlineWhenClose.Value.AsUtc() > utcNow;
    }
  }

  public bool CanBeAvailableForGuestTellers =>
    ListForPublic.AsBoolean()
    && ElectionPasscode.HasContent()
    && ListedForPublicAsOf.HasValue;

  // public long RowVersionInt
  // {
  //   get
  //   {
  //     if (C_RowVersion == null) return 0;
  //
  //     return BitConverter.ToInt64(C_RowVersion, 0);
  //   }
  // }

  private Dictionary<ExtraSettingKey, string> ExtraSettings
  {
    get
    {
      if (_extraDict != null) return _extraDict;
      // column contents...  ~Flag=1;FlagB=hello

      if (string.IsNullOrWhiteSpace(OwnerLoginId) || OwnerLoginId[0] != FlagChar)
        _extraDict = new Dictionary<ExtraSettingKey, string>();
      else
        _extraDict = OwnerLoginId
          .Substring(1) // skip flag char
          .Trim()
          .Split(SplitChar)
          .Select(s => s.Split('='))
          .Where(a => Enum.IsDefined(typeof(ExtraSettingKey), a[0]))
          // any that are not recognized are ignored and lost
          .ToDictionary(a => (ExtraSettingKey)Enum.Parse(typeof(ExtraSettingKey), a[0]), a => a[1]);

      return _extraDict;
    }
  }


  public bool VotingMethodsContains(string method)
  {
    return VotingMethods.DefaultTo("").Contains(method);
  }

  public bool VotingMethodsContains(VotingMethodEnum method)
  {
    return VotingMethods.DefaultTo("").Contains(method.Value);
  }

  private string GetExtraSetting(ExtraSettingKey setting)
  {
    if (ExtraSettings.TryGetValue(setting, out var value)) return value;

    return null;
  }

  private void SetExtraSetting(ExtraSettingKey setting, string value)
  {
    var s = value ?? "";
    if (s.Contains("=") || s.Contains(SplitChar))
      throw new ApplicationException("Invalid value for extra settings: " + s);

    var dict = ExtraSettings;

    if (s == "")
    {
      if (dict.ContainsKey(setting)) dict.Remove(setting);
    }
    else
    {
      dict[setting] = s;
    }

    if (dict.Count == 0)
      OwnerLoginId = null;
    else
      OwnerLoginId = FlagChar + dict.Select(kvp => kvp.Key + "=" + kvp.Value).JoinedAsString(SplitChar);

    _extraDict = dict;
  }

  private enum ExtraSettingKey
  {
    // keep names as short as possible
    BP, // Ballot Process?
    Env, // Envelope Mode
    T24, // use 24 hour time?
    RV, // randomize voters list displayed for voters
    GA, // Guest Tellers can add people
  }
}