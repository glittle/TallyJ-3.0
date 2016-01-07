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
  [Serializable]
  public partial class Election : IIndexedForCaching
  {
    public bool IsSingleNameElection
    {
      get { return NumberToElect.GetValueOrDefault(0) == 1 && NumberExtra.GetValueOrDefault(0) == 0; }
    }

    public bool ListForPublicCalculated {
      get
      {
        return ListForPublic.AsBoolean()
               && ElectionPasscode.HasContent()
               && ListedForPublicAsOf.HasValue;  //  <= 5.minutes(); // 2014-2-1 - time-out not working!
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
  }
}