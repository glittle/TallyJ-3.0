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
          .Select(e =>
          {
            return new
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
              e.EmailFromAddressWithDefault,
              e.EmailFromNameWithDefault,
              e.ElectionPasscode,
              e.OnlineEnabled,
              IsFuture = e.DateOfElection.HasValue && e.DateOfElection > DateTime.Today,
              IsCurrent = e.ElectionGuid == UserSession.CurrentElectionGuid,
              Type = ElectionTypeEnum.TextFor(e.ElectionType).DefaultTo("?"),
              Mode = ElectionModeEnum.TextFor(e.ElectionMode).SurroundContentWith(" (", ")"),
              IsTest = e.ShowAsTest.AsBoolean(),
              TallyStatusDisplay = ElectionTallyStatusEnum.Parse(e.TallyStatus).DisplayText,
              e.TallyStatus
            };
          }).ToList();
      }
    }

    public IEnumerable<object> MoreInfoStatic()
    {
      var electionGuids = MyElections().Select(e => e.ElectionGuid).ToList();

      var personCount = Db.Person.Where(p => electionGuids.Contains(p.ElectionGuid))
        .GroupBy(p => p.ElectionGuid)
        .Select(g => new { ElectionGuid = g.Key, Num = g.Count() })
        .ToDictionary(g => g.ElectionGuid, g => g.Num);

      var tellerCounts = Db.Teller.Where(l => electionGuids.Contains(l.ElectionGuid))
              .GroupBy(l => l.ElectionGuid)
              .Select(g => new { ElectionGuid = g.Key, Tellers = g.OrderBy(l => l.C_RowId) })
              .ToDictionary(g => g.ElectionGuid, g => g.Tellers.Select(t => t.Name));

      return electionGuids.Select(guid =>
      {
        personCount.TryGetValue(guid, out int numPeople);
        tellerCounts.TryGetValue(guid, out IEnumerable<string> tellers);

        return new
        {
          guid,
          numPeople,
          tellers,
        };
      });
    }

    public IEnumerable<object> MoreInfoLive()
    {
      var electionGuids = MyElections().Select(e => e.ElectionGuid).ToList();

      var ballotCount = Db.Location.Where(p => electionGuids.Contains(p.ElectionGuid))
        .Join(Db.Ballot, l => l.LocationGuid, b => b.LocationGuid, (l, b) => new { l.ElectionGuid, b })
        .GroupBy(p => p.ElectionGuid)
        .Select(g => new { ElectionGuid = g.Key, Num = g.Count() })
        .ToDictionary(g => g.ElectionGuid, g => g.Num);

      var logEntries = Db.C_Log
        // .Where(l => l.ElectionGuid != null)
        .Where(l => electionGuids.Contains(l.ElectionGuid.Value))
        .GroupBy(l => l.ElectionGuid)
        .Select(g => new { ElectionGuid = g.Key, Last = g.OrderByDescending(l => l.C_RowId).FirstOrDefault() })
        .ToDictionary(g => g.ElectionGuid, g => g.Last);

      var onlineVoterCounts = Db.OnlineVotingInfo.Where(ovi => electionGuids.Contains(ovi.ElectionGuid))
        .GroupBy(p => p.ElectionGuid)
        .ToList()
        .Select(g => new
        {
          ElectionGuid = g.Key,
          Voters = g.GroupBy(v => v.Status)
          .ToDictionary(gg => gg.Key,
            gg =>
              new OnlineVoterCount { Count = gg.Count(), AsOf = gg.Max(v => v.WhenStatus) })
        })
        .ToDictionary(g => g.ElectionGuid, g => g.Voters);


      return electionGuids.Select(guid =>
      {
        ballotCount.TryGetValue(guid, out int numBallots);
        logEntries.TryGetValue(guid, out C_Log lastLog);
        onlineVoterCounts.TryGetValue(guid, out Dictionary<string, OnlineVoterCount> onlineVoters);

        return new
        {
          guid,
          numBallots,
          lastLog,
          onlineVoters
        };
      });
    }

    private struct OnlineVoterCount
    {
      public int Count { get; set; }
      public DateTime? AsOf { get; set; }
    }


    public IEnumerable<Election> MyElections()
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