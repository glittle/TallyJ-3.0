using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using TallyJ.Code;

namespace TallyJ.EF
{
  [MetadataType(typeof(PersonMetadata))]
  [DebuggerDisplay("{FullName} {Voter}")]
  public partial class Person : IIndexedForCaching
  {
    /// <summary>
    /// Associated Voter record for this election. Manually loaded and saved.
    /// </summary>
    public Voter Voter { get; set; }
    //
    //
    // public bool? CanVote
    // {
    //   get
    //   {
    //     // check current election for Units
    //     var election = ItemKey.CurrentElection.FromPageItems(new Election());
    //
    //     return election.IsLsa2U && election.UnitName != UnitName
    //       ? false
    //       : CanVote;
    //   }
    //   set => CanVote = value;
    // }
    //
    // public bool? CanReceiveVotes
    // {
    //   get
    //   {
    //     // check current election for Units
    //     var election = ItemKey.CurrentElection.FromPageItems(new Election());
    //
    //     return election.IsLsa2U && election.UnitName != UnitName
    //       ? false
    //       : CanReceiveVotes;
    //   }
    //   set => CanReceiveVotes = value;
    // }
    //
    // enum ExtraSettingKey
    // {
    //   // keep names as short as possible
    //   RegLog, // registration log
    // }

    public int TempImportLineNum { get; set; }

    public string FullName
    {
      get
      {
        return new[]
        {
          LastName,
          OtherLastNames.SurroundContentWith(" [", "]"),
          FirstName.SurroundContentWith(", ", ""),
          OtherNames.SurroundContentWith(" [", "]"),
          OtherInfo.SurroundContentWith(" (", ")")
        }.JoinedAsString("", true);
      }
    }

    public string FullNameAndArea => FullNameFL + Area.SurroundContentWith(" <u>", "</u>");

    public string FullNameFL
    {
      get
      {
        return new[]
        {
          FirstName.SurroundContentWith("", " "),
          LastName,
          OtherNames.SurroundContentWith(" [", "]"),
          OtherLastNames.SurroundContentWith(" [", "]"),
          OtherInfo.SurroundContentWith(" (", ")")
        }.JoinedAsString("", true);
      }
    }

    private class PersonMetadata
    {
    }

    // /// <summary>
    // /// This is a "fake" column that is embedded into the old CombinedSoundCodes column.
    // /// </summary>
    // /// <remarks>
    // /// The values in the string must not contain <see cref="FlagChar"/> ~ or <see cref="SplitChar"/> ~ or <see cref="ArraySplit"/> |
    // /// </remarks>
    // public List<string> RegistrationLog
    // {
    //   get
    //   {
    //     var raw = GetExtraSetting(ExtraSettingKey.RegLog);
    //     return raw.HasContent() ? raw.Split(ArraySplit).ToList() : new List<string>();
    //   }
    //   set
    //   {
    //     SetExtraSetting(ExtraSettingKey.RegLog, value.JoinedAsString(ArraySplit));
    //   }
    // }

    //
    // const char FlagChar = '~';
    // const char SplitChar = '~';
    // const char ArraySplit = '|'; // for when the value is an array or List<string>
    //
    // private Dictionary<ExtraSettingKey, string> _extraDict;
    //
    // private Dictionary<ExtraSettingKey, string> ExtraSettings
    // {
    //   get
    //   {
    //     if (_extraDict != null)
    //     {
    //       return _extraDict;
    //     }
    //     // column contents...  ~Flag=1;FlagB=hello
    //
    //     if (string.IsNullOrWhiteSpace(CombinedSoundCodes) || CombinedSoundCodes[0] != FlagChar)
    //     {
    //       _extraDict = new Dictionary<ExtraSettingKey, string>();
    //     }
    //     else
    //     {
    //       _extraDict = CombinedSoundCodes
    //         .Substring(1) // skip flag char
    //         .Trim()
    //         .Split(SplitChar)
    //         .Select(s => s.Split('='))
    //         .Where(a => Enum.IsDefined(typeof(ExtraSettingKey), a[0]))
    //           // any that are not recognized are ignored and lost
    //           .ToDictionary(a => (ExtraSettingKey)Enum.Parse(typeof(ExtraSettingKey), a[0]), a => a[1]);
    //     }
    //
    //     return _extraDict;
    //   }
    // }
    //
    //
    // private string GetExtraSetting(ExtraSettingKey setting)
    // {
    //   string value;
    //   if (ExtraSettings.TryGetValue(setting, out value))
    //   {
    //     return value;
    //   }
    //   return null;
    // }
    //
    // private void SetExtraSetting(ExtraSettingKey setting, string value)
    // {
    //   var s = value ?? "";
    //   if (s.Contains("=") || s.Contains(SplitChar))
    //   {
    //     throw new ApplicationException("Invalid value for extra settings: " + s);
    //   }
    //
    //   var dict = ExtraSettings;
    //
    //   if (s == "")
    //   {
    //     if (dict.ContainsKey(setting))
    //     {
    //       dict.Remove(setting);
    //     }
    //   }
    //   else
    //   {
    //     dict[setting] = s;
    //   }
    //
    //   if (dict.Count == 0)
    //   {
    //     CombinedSoundCodes = null;
    //   }
    //   else
    //   {
    //     CombinedSoundCodes = FlagChar + dict.Select(kvp => kvp.Key + "=" + kvp.Value).JoinedAsString(SplitChar);
    //   }
    //
    //   _extraDict = dict;
    // }
  }
}