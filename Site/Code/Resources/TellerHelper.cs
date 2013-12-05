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
    public IEnumerable<Teller> Tellers
    {
      get
      {
        return new TellerCacher().AllForThisElection;
      }
    }

    public string GetTellerOptions(int i)
    {
      var tellerGuid = UserSession.GetCurrentTeller(i);

      return Tellers
        .OrderBy(l => l.Name)
        .Select(l => new { l.C_RowId, l.Name, Selected = l.TellerGuid == tellerGuid ? " selected" : "" })
        .Select(l => "<option value={C_RowId}{Selected}>{Name}</option>".FilledWith(l))
        .JoinedAsString()
        .SurroundWith("<option value='0'>(Select teller...)</option>", "<option value='-1'>+ Add teller name</option>");
    }
  }
}