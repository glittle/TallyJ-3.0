using System;
using System.Linq;
using EntityFramework.Extensions;
using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;

namespace TallyJ.EF
{
  [Serializable]
  public partial class Election
  {
    public bool IsSingleNameElection
    {
      get { return NumberToElect.GetValueOrDefault(0) == 1 && NumberExtra.GetValueOrDefault(0) == 0; }
    }

    /// <Summary>Erase all ballots and results</Summary>
    public static void EraseBallotsAndResults(Guid electionGuid)
    {
      var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

      db.Result.Delete(r => r.ElectionGuid == electionGuid);
      db.ResultTie.Delete(r => r.ElectionGuid == electionGuid);
      db.ResultSummary.Delete(r => r.ElectionGuid == electionGuid);

      // delete ballots in all locations... cascading will delete votes
      db.Ballot.Delete(b => Location.AllLocationsCached.Select(l => l.LocationGuid).Contains(b.LocationGuid));
    }

  }
}