using System;
using System.Linq;
using EntityFramework.Extensions;
using TallyJ.Code.Data;
using TallyJ.Code.UnityRelated;

namespace TallyJ.Models
{
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

      db.Results.Delete(r => r.ElectionGuid == electionGuid);
      db.ResultTies.Delete(r => r.ElectionGuid == electionGuid);
      db.ResultSummaries.Delete(r => r.ElectionGuid == electionGuid);

      // delete ballots in all locations... cascading will delete votes
      db.Ballots.Delete(b => db.Locations.Where(l => l.ElectionGuid == electionGuid).Select(l => l.LocationGuid).Contains(b.LocationGuid));
    }

  }
}