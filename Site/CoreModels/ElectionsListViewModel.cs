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
        // .OrderBy(e => e.ShowAsTest.AsBoolean())
        // .ThenByDescending(e => e.DateOfElection)
        // .ThenBy(e => e.Name)

        return MyElections()
          .Select(e => new
          {
            e.Name,
            e.ElectionGuid,
            e.DateOfElection,
            e.ElectionType,
            e.ElectionMode,
            e.ShowAsTest,
            e.IsSingleNameElection,
            e.CanBeAvailableForGuestTellers,
            e.OnlineCurrentlyOpen,
            e.OnlineWhenOpen,
            e.OnlineWhenClose,
            IsFuture = e.DateOfElection.HasValue && e.DateOfElection > DateTime.Today,
            IsCurrent = e.ElectionGuid == UserSession.CurrentElectionGuid,
            Type = ElectionTypeEnum.TextFor(e.ElectionType).DefaultTo("?"),
            Mode = ElectionModeEnum.TextFor(e.ElectionMode).SurroundContentWith(" (", ")"),
            IsTest = e.ShowAsTest.AsBoolean(),
            TallyStatus = ElectionTallyStatusEnum.TextFor(e.TallyStatus, e.TallyStatus),
          }).ToList();
      }
    }

    public IEnumerable<object> ElectionCounts()
    {
      var electionGuids = MyElections().Select(e => e.ElectionGuid).ToList();

      var personCount = Db.Person.Where(p => electionGuids.Contains(p.ElectionGuid))
        .GroupBy(p => p.ElectionGuid)
        .Select(g => new { ElectionGuid = g.Key, Num = g.Count() })
        .ToDictionary(g => g.ElectionGuid, g => g.Num);

      var ballotCount = Db.Location.Where(p => electionGuids.Contains(p.ElectionGuid))
        .Join(Db.Ballot, l => l.LocationGuid, b => b.LocationGuid, (l, b) => new { l.ElectionGuid, b })
        .GroupBy(p => p.ElectionGuid)
        .Select(g => new { ElectionGuid = g.Key, Num = g.Count() })
        .ToDictionary(g => g.ElectionGuid, g => g.Num);

      return electionGuids.Select(guid =>
      {
        personCount.TryGetValue(guid, out int pc);
        ballotCount.TryGetValue(guid, out int bc);

        return new
        {
          guid,
          numPeople = pc,
          numBallots = bc
        };
      });
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