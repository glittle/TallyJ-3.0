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
    public IEnumerable<object> GetMyElectionsInfo(bool checkIfAddedToNew = false) {

      if (checkIfAddedToNew && UserSession.IsKnownTeller)
      {
        CheckIfAddedToNew();
      }

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
          OnlineWhenOpen = e.OnlineWhenOpen.AsUtc(),
          OnlineWhenClose = e.OnlineWhenClose.AsUtc(),
          e.EmailFromAddressWithDefault,
          e.EmailFromNameWithDefault,
          e.ElectionPasscode,
          e.OnlineEnabled,
          IsFuture = e.DateOfElection.HasValue && e.DateOfElection.AsUtc() > DateTime.UtcNow,
          IsCurrent = e.ElectionGuid == UserSession.CurrentElectionGuid,
          Type = ElectionTypeEnum.TextFor(e.ElectionType).DefaultTo("?"),
          Mode = ElectionModeEnum.TextFor(e.ElectionMode).SurroundContentWith(" (", ")"),
          IsTest = e.ShowAsTest.AsBoolean(),
          TallyStatusDisplay = ElectionTallyStatusEnum.Parse(e.TallyStatus).DisplayText,
          e.TallyStatus
        });
    }

    private void CheckIfAddedToNew()
    {
      // find any unclaimed ones with my email address
      var email = UserSession.AdminAccountEmail;

      var pendingInvitations = Db.JoinElectionUser
        .Where(u => u.UserId == Guid.Empty && u.InviteEmail == email);

      // claim them!
      foreach (var pendingInvitation in pendingInvitations)
      {
        pendingInvitation.UserId = UserSession.UserGuid;
        new LogHelper(pendingInvitation.ElectionGuid).Add($"Activated full teller invitation - {email}", true);
      }

      Db.SaveChanges();
    }

    public IEnumerable<object> MoreInfoStatic()
    {
      var electionGuids = MyElections().Select(e => e.ElectionGuid).ToList();

      var personCount = Db.Person.Where(p => electionGuids.Contains(p.ElectionGuid))
        .GroupBy(p => p.ElectionGuid)
        .Select(g => new { ElectionGuid = g.Key, Num = g.Count(p => p.CanVote.Value) })
        .ToDictionary(g => g.ElectionGuid, g => g.Num);

      var tellerCounts = Db.Teller.Where(l => electionGuids.Contains(l.ElectionGuid))
              .GroupBy(l => l.ElectionGuid)
              .Select(g => new { ElectionGuid = g.Key, Tellers = g.OrderBy(l => l.C_RowId) })
              .ToDictionary(g => g.ElectionGuid, g => g.Tellers.Select(t => t.Name));

      var knownUsers = Db.JoinElectionUser.Where(jeu => electionGuids.Contains(jeu.ElectionGuid))
        .Join(Db.Users, j => j.UserId, u => u.UserId, (j, u) => new { j, u, u.LastActivityDate })
        .GroupJoin(Db.Memberships, j => j.u.UserId, j => j.UserId, (j, mList) => new { j, Email = mList.FirstOrDefault().Email })
        .Select(j => new
        {
          j.j.j.ElectionGuid,
          j.j.LastActivityDate,
          j.j.j.Role,
          j.j.j.InviteWhen,
          j.j.j.InviteEmail,
          j.j.j.C_RowId,
          j.Email,
          j.j.u.UserId,
          j.j.u.UserName,
        })
        .ToList()
        .GroupBy(l => l.ElectionGuid)
        .Select(g => new
        {
          ElectionGuid = g.Key,
          Users = g.OrderBy(u => u.Role).ThenBy(u => u.UserName).Select(u => (object)new
          {
            u.Role,
            InviteWhen = u.InviteWhen.AsUtc(),
            u.InviteEmail,
            u.C_RowId,
            u.Email,
            u.UserName,
            LastActivityDate = u.UserId == Guid.Empty ? (DateTime?)null : u.LastActivityDate.AsUtc(),
            isCurrentUser = u.UserId == UserSession.UserGuid
          })
        })
        .ToDictionary(g => g.ElectionGuid, g => g.Users);


      return electionGuids.Select(guid =>
      {
        personCount.TryGetValue(guid, out int numPeople);
        tellerCounts.TryGetValue(guid, out IEnumerable<string> tellers);
        knownUsers.TryGetValue(guid, out IEnumerable<object> users);

        return new
        {
          guid,
          numPeople,
          tellers,
          users
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


      var registeredCounts = Db.Person.Where(p => electionGuids.Contains(p.ElectionGuid))
        .GroupBy(p => p.ElectionGuid)
        .Select(g => new { ElectionGuid = g.Key, Num = g.Count(p => !string.IsNullOrEmpty(p.VotingMethod)) })
        .ToDictionary(g => g.ElectionGuid, g => g.Num);


      return electionGuids.Select(guid =>
      {
        ballotCount.TryGetValue(guid, out int numBallots);
        logEntries.TryGetValue(guid, out C_Log lastLog);
        onlineVoterCounts.TryGetValue(guid, out Dictionary<string, OnlineVoterCount> onlineVoters);
        registeredCounts.TryGetValue(guid, out int numRegistered);

        return new
        {
          guid,
          numBallots,
          onlineVoters,
          numRegistered,
          lastLog,
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