using System;
using System.Collections.Generic;
using System.Linq;
using EntityFramework.Caching;
using EntityFramework.Extensions;
using TallyJ.Code;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.EF
{
  enum ExtraSettingKey
  {
    // keep names as short as possible
    Process, // use pre-ballot pages?
  }

  public enum BallotProcessKey
  {
    Unknown,
    None,
    RC, // register, then roll call (no confirmation)
    Reg, // register, collect
    NoReg, // no pre-registration, just collect 
  }

  [Serializable]
  public partial class Election : IIndexedForCaching
  {
    // fake column, embedded into OwnerLoginId
    /// <summary>
    /// must be a string to serialize out to client
    /// </summary>
    public string BallotProcess
    {
      get
      {
        return GetExtraSettting(ExtraSettingKey.Process);
      }
      set
      {
        if (value != null && !Enum.IsDefined(typeof(BallotProcessKey), value)) {
          throw new ApplicationException("Invalid process key: " + value);
        }
        SetExtraSettting(ExtraSettingKey.Process, value);
      }
    }

    //public string Test2
    //{
    //  // Replace this when a second fake field is created!

    //  get
    //  {
    //    return GetExtraSettting(ExtraSettingKey.Test2);
    //  }
    //  set
    //  {
    //    SetExtraSettting(ExtraSettingKey.Test2, value);
    //  }
    //}


    public bool IsSingleNameElection
    {
      get { return NumberToElect.GetValueOrDefault(0) == 1 && NumberExtra.GetValueOrDefault(0) == 0; }
    }

    public bool CanBeAvailableForGuestTellers
    {
      get
      {
        return ListForPublic.AsBoolean()
               && ElectionPasscode.HasContent()
               && ListedForPublicAsOf.HasValue;
      }
    }

    /// <Summary>Erase all ballots and results</Summary>
    public static void EraseBallotsAndResults(Guid electionGuid)
    {
      var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

      db.Result.Where(r => r.ElectionGuid == electionGuid).Delete();
      db.ResultTie.Where(r => r.ElectionGuid == electionGuid).Delete();
      db.ResultSummary.Where(r => r.ElectionGuid == electionGuid).Delete();

      // delete ballots in all locations... cascading will delete votes
      db.Ballot.Where(b => db.Location.Where(x => x.ElectionGuid == electionGuid).Select(l => l.LocationGuid).Contains(b.LocationGuid)).Delete();
    }

    public long RowVersionInt
    {
      get
      {
        if (C_RowVersion == null)
        {
          return 0;
        }
        return BitConverter.ToInt64(C_RowVersion, 0);
      }
    }


    const char FlagChar = '~';
    const char SplitChar = ';';

    private Dictionary<ExtraSettingKey, string> ExtraSettings
    {
      get
      {
        // column contents...  ~Flag=1;FlagB=hello

        if (string.IsNullOrWhiteSpace(OwnerLoginId) || OwnerLoginId[0] != FlagChar) return new Dictionary<ExtraSettingKey, string>();
        return OwnerLoginId
        .Substring(1) // skip flag char
        .Trim()
        .Split(SplitChar)
        .Select(s => s.Split('='))
        .ToDictionary(a => (ExtraSettingKey)Enum.Parse(typeof(ExtraSettingKey), a[0]), a => a[1]);
      }
    }
    private string GetExtraSettting(ExtraSettingKey setting)
    {
      string value;
      if (ExtraSettings.TryGetValue(setting, out value))
      {
        return value;
      }
      return null;
    }

    private void SetExtraSettting(ExtraSettingKey setting, string value)
    {
      var s = value ?? "";
      if (s.Contains("=") || s.Contains(SplitChar))
      {
        throw new ApplicationException("Invalid value for extra settings: " + s);
      }

      var dict = ExtraSettings;

      if (s == "")
      {
        if (dict.ContainsKey(setting))
        {
          dict.Remove(setting);
        }
      }
      else
      {
        dict[setting] = s;
      }

      if (dict.Count == 0)
      {
        OwnerLoginId = null;
      }
      else
      {
        OwnerLoginId = FlagChar + dict.Select(kvp => kvp.Key + "=" + kvp.Value).JoinedAsString(SplitChar);
      }
    }


  }
}