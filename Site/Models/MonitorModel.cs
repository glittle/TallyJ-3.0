using System;
using System.Linq;
using TallyJ.Code;
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
        return
          new
            {
              Locations = Db.vLocationInfoes
                .Where(li => li.ElectionGuid == UserSession.CurrentElectionGuid)
                .OrderBy(li => li.SortOrder)
                .ThenBy(li => li.ComputerCode)
                .ThenBy(li => li.C_RowId)
                .ToList()
                .Select(li => new
                                {
                                  li.Ballots,
                                  li.BallotsCollected,
                                  li.ComputerCode,
                                  li.ContactInfo,
                                  li.Name,
                                  li.TallyStatus,
                                  li.TellerName,
                                  MinutesOld = li.LastContact.HasValue ? ((now - li.LastContact.Value).TotalSeconds / 60).ToString("0.0") : "",
                                  LocationId = li.C_RowId
                                })
            };
      }
    }
  }
}