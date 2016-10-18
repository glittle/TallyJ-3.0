using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ElectionsListViewModel : DataConnectedModel
  {
    public IEnumerable<object> MyElectionsInfo
    {
      get
      {
        var list = MyElections()
          .OrderBy(e => e.ShowAsTest.AsBoolean())
          .ThenByDescending(e => e.DateOfElection)
          .ThenBy(e => e.Name)
          .Select(e => new
          {
            e.Name,
            e.ElectionGuid,
            e.DateOfElection,
            e.ElectionType,
            e.ElectionMode,
            e.ShowAsTest,
            e.IsSingleNameElection
          }).ToList();

        var electionGuids = list.Select(e => e.ElectionGuid).ToList();

        var personCount = Db.Person.Where(p => electionGuids.Contains(p.ElectionGuid))
          .GroupBy(p => p.ElectionGuid)
          .Select(g => new { ElectionGuid = g.Key, Num = g.Count() })
          .ToList();

        var ballotCount = Db.Location.Where(p => electionGuids.Contains(p.ElectionGuid))
          .Join(Db.Ballot, l => l.LocationGuid, b => b.LocationGuid, (l, b) => new { l.ElectionGuid, b })
          .GroupBy(p => p.ElectionGuid)
          .Select(g => new { ElectionGuid = g.Key, Num = g.Count() })
          .ToList();

        return list.Select(info =>
                      {
                        var isCurrent = info.ElectionGuid == UserSession.CurrentElectionGuid;
                        var personCounts = personCount.SingleOrDefault(c => c.ElectionGuid == info.ElectionGuid);
                        var ballotCounts = ballotCount.SingleOrDefault(c => c.ElectionGuid == info.ElectionGuid);
                        return new
                              {
                                info.Name,
                                info.ElectionGuid,
                                DateOfElection =
                                    info.DateOfElection.HasValue
                                        ? info.DateOfElection.AsString("yyyy-MMM-dd")
                                        : "",
                                IsFuture = info.DateOfElection.HasValue && info.DateOfElection > DateTime.Today,
                                IsCurrent = isCurrent,
                                // Locations = isCurrent ? locations : null,
                                Type = ElectionTypeEnum.TextFor(info.ElectionType),
                                Mode =
                                    ElectionModeEnum.TextFor(info.ElectionMode).SurroundContentWith(" (", ")"),
                                IsTest = info.ShowAsTest.AsBoolean(),
                                info.IsSingleNameElection,
                                NumVoters = personCounts == null ? 0 : personCounts.Num,
                                NumBallots = ballotCounts == null ? 0 : ballotCounts.Num
                              };
                      });
      }
    }

    private IEnumerable<Election> MyElections()
    {
      if (UserSession.IsKnownTeller)
      {
        var userGuid = UserSession.UserGuid;
        return Db.Election
          .SelectMany(e => Db.JoinElectionUser.Where(j => j.UserId == userGuid),
            (e, j) => new { e, j })
          .Where(joined => joined.j.ElectionGuid.Equals(joined.e.ElectionGuid))
          .Select(joined => joined.e)
          .ToList();
      }

      var currentElection = UserSession.CurrentElection;
      if (UserSession.IsGuestTeller && currentElection != null)
      {
        return new List<Election> { currentElection };
      }

      // not logged in correctly

      return new List<Election>();
    }

  }
}