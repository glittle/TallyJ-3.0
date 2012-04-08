using System;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;

namespace TallyJ.Models
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

        return
          new
            {
              Locations = Db.vLocationInfoes
                .Where(li => li.ElectionGuid == currentElectionGuid)
                .OrderBy(li => li.SortOrder)
                .ThenBy(li => li.ComputerCode)
                .ThenBy(li => li.C_RowId)
                .ToList()
                .Select(li => new
                                {
                                  li.BallotsAtComputer,
                                  li.BallotsAtLocation,
                                  li.BallotsCollected,
                                  li.ComputerCode,
                                  li.ContactInfo,
                                  li.Name,
                                  TallyStatus = LocationStatusEnum.TextFor(li.TallyStatus),
                                  li.TellerName,
                                  MinutesOld = li.LastContact.HasValue ? ((now - li.LastContact.Value).TotalSeconds / 60).ToString("0.0") : "",
                                  LocationId = li.C_RowId
                                })
                                ,
              Ballots = Db.vBallotInfoes
                .Where(bi => bi.ElectionGuid == currentElectionGuid  && (bi.StatusCode == BallotStatusEnum.Review || bi.VotesChanged > 0))
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