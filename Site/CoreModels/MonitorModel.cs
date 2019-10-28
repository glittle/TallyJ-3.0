using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.EF;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;

namespace TallyJ.CoreModels
{
  public class MonitorModel : DataConnectedModel
  {
    public string MonitorInfoJson
    {
      get { return MonitorInfo.SerializedAsJsonString(); }
    }

    public object MonitorInfo
    {
      get
      {
        var now = DateTime.Now;

        var ballots = new BallotCacher(Db).AllForThisElection;
        var currentElection = UserSession.CurrentElection;
        var isSingleName = currentElection.IsSingleNameElection;
        var locations = new LocationCacher(Db).AllForThisElection;
        var votes = new VoteCacher(Db).AllForThisElection;

        var currentElectionGuid = UserSession.CurrentElectionGuid;

        var onlineBallots = SettingsHelper.HostSupportsOnlineElections
          ? Db.Person
            .Where(p => p.ElectionGuid == currentElectionGuid && p.Email != null)
            .GroupJoin(Db.OnlineVotingInfo, p => new { p.PersonGuid, p.ElectionGuid },
               ovi => new { ovi.PersonGuid, ovi.ElectionGuid }, (p, oviList) =>
                new { p, ovi = oviList.FirstOrDefault() })
            .Select(j => new
            {
              j.p.VotingMethod,
              j.p.C_FullName,
              j.p.C_RowId,
              j.ovi,
              j.p.Email
            })
            .OrderBy(j => j.C_FullName)
            .ToList()
            .Select(j => new
            {
              j.ovi?.Status,
              j.ovi?.WhenStatus,
              j.ovi?.HistoryStatus,
              votesReady = j.ovi != null && (j.ovi.PoolLocked.GetValueOrDefault()
                                             //use JSON or do not check length of pool name
                                             //&& j.ovi.ListPool?.Split(',').Length >= currentElection.NumberToElect
                                             ),
              VotingMethod = j.VotingMethod,
              VotingMethod_Display = VotingMethodEnum.TextFor(j.VotingMethod).DefaultTo("-"),
              j.C_FullName,
              PersonId = j.C_RowId,
              j.Email
            })
          : null;

        var locationResult = locations
          .Where(l => !l.IsTheOnlineLocation)
          .OrderBy(l => l.SortOrder)
          .ThenBy(l => l.Name)
          .ThenBy(l => l.C_RowId)
          .Select(l => new
          {
            BallotsAtLocation = new BallotHelper().BallotCount(l.LocationGuid, isSingleName, ballots, votes),
            l.BallotsCollected,
            l.ContactInfo,
            l.Name,
            TallyStatus = LocationStatusEnum.TextFor(l.TallyStatus),
            LocationId = l.C_RowId,
            BallotCodes = ballots.Where(b => b.LocationGuid == l.LocationGuid).GroupBy(b => b.ComputerCode)
              .OrderBy(g => g.Key)
              .Select(g => new
              {
                ComputerCode = g.Key,
                BallotsAtComputer = new BallotHelper().BallotCount(l.LocationGuid, g.Key, isSingleName, ballots, votes).ToString(),
                Computers = new ComputerCacher().AllForThisElection.Where(c => c.ComputerCode == g.Key && c.LocationGuid == l.LocationGuid)
                  .OrderBy(c => c.ComputerCode)
                  .Select(c => new
                  {
                    Tellers = c.GetTellerNames().DefaultTo("(not set)"),
                    SecondsOld = c.LastContact.HasValue ? ((now - c.LastContact.Value).TotalSeconds).ToString("0") : "",
                  })
              })
          })
          .ToList();

        var ballotResult = ballots
          .Where(BallotAnalyzer.BallotNeedsReview)
          .Join(locations, b => b.LocationGuid, l => l.LocationGuid, (b, l) => new { b, l })
          .OrderBy(g => g.b.ComputerCode)
          .ThenBy(g => g.b.BallotNumAtComputer)
          .Select(g =>
            new
            {
              Id = g.b.C_RowId,
              Code = g.b.C_BallotCode,
              Status = BallotStatusEnum.TextFor(g.b.StatusCode),
              LocationName = g.l.Name,
              LocationId = g.l.C_RowId,
              Tellers = TellerModel.GetTellerNames(g.b.Teller1, g.b.Teller2)
            })
          .ToList();

        return
          new
          {
            Locations = locationResult,
            Ballots = ballotResult,
            OnlineInfo = SettingsHelper.HostSupportsOnlineElections
              ? new
              {
                currentElection.OnlineWhenOpen,
                currentElection.OnlineWhenClose,
                currentElection.OnlineCloseIsEstimate,
              }
              : null,
            OnlineBallots = onlineBallots
          };
      }
    }
  }
}