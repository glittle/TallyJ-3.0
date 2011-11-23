using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class MonitorModel : DataConnectedModel
  {
    public string LocationInfoJson
    {
      get
      {
        return LocationInfo.SerializedAsJson();
      }
    }

    public IQueryable<vLocationInfo> LocationInfo
    {
      get
      {
        return Db.vLocationInfoes
          .OrderBy(li => li.SortOrder)
          .ThenBy(li => li.C_RowId)
          .Where(li => li.ElectionGuid == UserSession.CurrentElectionGuid);
      }
    }
  }
}