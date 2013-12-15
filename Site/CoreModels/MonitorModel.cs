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

        var ballots = new BallotCacher().AllForThisElection;
        var isSingleName = UserSession.CurrentElection.IsSingleNameElection;
        var locations = new LocationCacher().AllForThisElection;

        return
          new
            {
              Locations = locations
                .Join(new ComputerCacher().AllForThisElection, l => l.LocationGuid, c => c.LocationGuid, (l, c) => new { l, c })
                .OrderBy(g => g.l.SortOrder)
                .ThenBy(g => g.c.ComputerCode)
                .ThenBy(g => g.l.C_RowId)
                .Select(g => new
                                {
                                  BallotsAtComputer = BallotModelCore.BallotCount(g.l.LocationGuid, g.c.ComputerCode, isSingleName, ballots),
                                  BallotsAtLocation = BallotModelCore.BallotCount(g.l.LocationGuid, isSingleName, ballots),
                                  g.l.BallotsCollected,
                                  g.c.ComputerCode,
                                  g.l.ContactInfo,
                                  g.l.Name,
                                  TallyStatus = LocationStatusEnum.TextFor(g.l.TallyStatus),
                                  Teller = g.c.GetTellerNames(),
                                  MinutesOld = g.c.LastContact.HasValue ? ((now - g.c.LastContact.Value).TotalSeconds / 60).ToString("0.0") : "",
                                  LocationId = g.l.C_RowId
                                })
                                ,
              Ballots = ballots//.Select(b=>new BallotInfo(b, null))
                .Where(bi => bi.StatusCode == BallotStatusEnum.Review || bi.StatusCode == BallotStatusEnum.Verify)
                .Join(locations, b => b.LocationGuid, l => l.LocationGuid, (b, l) => new { b, l })
//                .JoinMatchingOrNull(tellers, g => g.b.TellerAtKeyboard, t => t.TellerGuid, (g, t) => new { g.b, g.l, TellerAtKeyboardName = t == null ? null : t.Name })
//                .JoinMatchingOrNull(tellers, g => g.b.TellerAssisting, t => t.TellerGuid, (g, t) => new { g.b, g.l, g.TellerAtKeyboardName, TellerAssistingName = t == null ? null : t.Name })
                .OrderBy(g => g.b.C_RowId)
                //.ToList()
                .Select(g =>
                new
                  {
                    Id = g.b.C_RowId,
                    Code = g.b.C_BallotCode,
                    Status = BallotStatusEnum.TextFor(g.b.StatusCode),
                    LocationName = g.l.Name,
                    LocationId = g.l.C_RowId,
                    Tellers = Computer.GetTellerNames(g.b.TellerAtKeyboard, g.b.TellerAssisting)
                  })
            };
      }
    }
  }
}