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
        var currentElectionGuid = UserSession.CurrentElectionGuid;

        var votes = new VoteCacher().AllForThisElection;
        var ballots = new BallotCacher().AllForThisElection;
        var isSingleName = UserSession.CurrentElection.IsSingleNameElection;

        return
          new
            {
              ComputerInfo = new LocationCacher().AllForThisElection
                .Join(new ComputerCacher().AllForThisElection, l => l.LocationGuid, c => c.LocationGuid, (l, c) => new { l, c })
                .OrderBy(g => g.l.SortOrder)
                .ThenBy(g => g.c.ComputerCode)
                .ThenBy(g => g.l.C_RowId)
                .ToList()
                .Select(g => new
                                {
                                  BallotsAtComputer = BallotModelCore.BallotCount(g.l.LocationGuid, g.c.ComputerCode, isSingleName),
                                  BallotsAtLocation = BallotModelCore.BallotCount(g.l.LocationGuid, isSingleName),
                                  g.l.BallotsCollected,
                                  g.c.ComputerCode,
                                  g.l.ContactInfo,
                                  g.l.Name,
                                  TallyStatus = LocationStatusEnum.TextFor(g.l.TallyStatus),
                                  TellerName = g.c.GetTellerName(),
                                  MinutesOld = g.c.LastContact.HasValue ? ((now - g.c.LastContact.Value).TotalSeconds / 60).ToString("0.0") : "",
                                  LocationId = g.l.C_RowId
                                })
                                ,
              Ballots = new List<BallotInfo>()
                .Where(bi => bi.StatusCode == BallotStatusEnum.Review || bi.VotesChanged > 0)
                .OrderBy(bi => bi.C_RowId)
                .ToList()
                .Select(bi =>
                new
                  {
                    Id = bi.C_RowId,
                    Code = bi.C_BallotCode,
                    Status = bi.VotesChanged > 0 ? "Verification Needed" : BallotStatusEnum.Review.DisplayText,
                    bi.LocationName,
                    bi.LocationId,
                    bi.TellerAtKeyboardName,
                    bi.TellerAssistingName
                  })
            };
      }
    }
  }
}