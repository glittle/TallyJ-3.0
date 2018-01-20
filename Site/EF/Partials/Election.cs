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
  enum ExtraSetting
  {
    // keep names as short as possible
    PreB // use pre-ballot pages?
  }

  [Serializable]
  public partial class Election : IIndexedForCaching
  {
    const char FlagChar = '~';
    const char SplitChar = ';';

    private Dictionary<ExtraSetting, string> ExtraSettings
    {
      get
      {
        // column contents...  ~Flag=1;FlagB=hello

        if (string.IsNullOrWhiteSpace(OwnerLoginId) || OwnerLoginId[0] != FlagChar) return new Dictionary<ExtraSetting, string>();
        return OwnerLoginId
        .Substring(1) // skip flag char
        .Split(SplitChar)
        .Select(s => s.Split('='))
        .ToDictionary(a => (ExtraSetting)Enum.Parse(typeof(ExtraSetting), a[0]), a => a[1]);
      }
    }
    private string GetExtraSettting(ExtraSetting setting)
    {
      string value;
      if (ExtraSettings.TryGetValue(setting, out value)) {
        return value;
      }
      return null;
    }

    private void SetExtraSettting(ExtraSetting setting, object value)
    {
      ExtraSettings[setting] = value.ToString();
      OwnerLoginId = FlagChar + ExtraSettings.Select(kvp => kvp.Key + "=" + kvp.Value).JoinedAsString(SplitChar);
    }

    // fake column, embedded into OwnerLoginId
    public bool UsePreBallot
    {
      get
      {
        return GetExtraSettting(ExtraSetting.PreB).AsBoolean();
      }
      set
      {
        SetExtraSettting(ExtraSetting.PreB, value ? 1 : 0);
      }
    }
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
  }
}