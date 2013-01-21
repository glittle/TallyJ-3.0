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
    private IEnumerable<Teller> _tellers;

    public IEnumerable<Teller> Tellers
    {
      get
      {
        return _tellers ?? (_tellers = Db.Tellers.Where(l => l.ElectionGuid == UserSession.CurrentElectionGuid).ToList());
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

    public void RefreshTellerList()
    {
      _tellers = null;
    }
  }
}