using System;
using System.Linq;

namespace TallyJ.EF
{
  [Serializable]
  public partial class Computer
  {
    public string GetTellerName()
    {
      var teller = Teller.AllTellersCached.SingleOrDefault(t => t.TellerGuid == Teller1)
                   ?? Teller.AllTellersCached.SingleOrDefault(t => t.TellerGuid == Teller2);
      return teller == null ? "" : teller.Name;
    }
  }
}