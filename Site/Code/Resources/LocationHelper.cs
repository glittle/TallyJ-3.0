using System.Collections.Generic;
using System.Linq;
using System.Web;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Code.Resources
{
  public class LocationHelper : DataConnectedModel
  {
    private List<Location> _locations;

    private List<Location> Locations
    {
      get
      {
        return _locations ?? (_locations = Db.Locations.Where(l => l.ElectionGuid == UserSession.CurrentElectionGuid).ToList());
      }
    }

    public string ShowDisabled
    {
      get { return Locations.Count == 1 ? " disabled" : ""; }
    }

    public HtmlString GetLocationOptions()
    {
      var currentLocation = UserSession.CurrentLocation;
      var selected = 0;
      if (currentLocation != null)
      {
        selected = currentLocation.C_RowId;
      }

      return Locations
        .OrderBy(l => l.SortOrder)
        .Select(l => new { l.C_RowId, l.Name, Selected = l.C_RowId == selected ? " selected" : "" })
        .Select(l => "<option value={C_RowId}{Selected}>{Name}</option>".FilledWith(l))
        .JoinedAsString()
        .AsRawHtml();
    }

  }
}