using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Code.Resources
{
  public class TellerHelper : DataConnectedModel
  {
    public string GetTellerOptions(int tellerIdToSelect)
    {
      var tellerGuid = UserSession.GetCurrentTeller(tellerIdToSelect);

      return new TellerCacher().AllForThisElection
        .OrderBy(l => l.Name)
        .Select(l => new { l.C_RowId, l.Name, Selected = l.TellerGuid == tellerGuid ? " selected" : "" })
        .Select(l => "<option value={C_RowId}{Selected}>{Name}</option>".FilledWith(l))
        .JoinedAsString()
        .SurroundWith("<option value='0'>Enter teller's name!</option>", "<option value='-1'>+ Add my name</option>");
    }
  }
}