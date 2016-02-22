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
    public string LocationInfoJson
    {
      get { return LocationInfo.SerializedAsJsonString(); }
    }

    public object LocationInfo
    {
      get
      {
        var now = DateTime.Now;

        var ballots = new BallotCacher(Db).AllForThisElection;
        var isSingleName = UserSession.CurrentElection.IsSingleNameElection;
        var locations = new LocationCacher(Db).AllForThisElection;
        var votes = new VoteCacher(Db).AllForThisElection;

        return
          new
            {
              Locations = locations
                //.JoinMatchingOrNull(new ComputerCacher(Db).AllForThisElection, l => l.LocationGuid, c => c.LocationGuid, (l, c) => new { l, c })
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
                                ,
              Ballots = ballots
                .Where(bi => bi.StatusCode == BallotStatusEnum.Review || bi.StatusCode == BallotStatusEnum.Verify)
                .Join(locations, b => b.LocationGuid, l => l.LocationGuid, (b, l) => new { b, l })
                .OrderBy(g => g.b.C_RowId)
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
            };
      }
    }
  }
}