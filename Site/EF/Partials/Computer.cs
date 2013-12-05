using System;
using System.Linq;

namespace TallyJ.EF
{
  [Serializable]
  public partial class Computer : IIndexedForCaching
  {
    public string GetTellerName()
    {
      var tellers = new TellerCacher().AllForThisElection;

      var teller = tellers.SingleOrDefault(t => t.TellerGuid == Teller1)
                   ?? tellers.SingleOrDefault(t => t.TellerGuid == Teller2);
      return teller == null ? "" : teller.Name;
    }
  }
}