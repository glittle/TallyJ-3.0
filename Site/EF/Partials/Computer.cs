using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;

namespace TallyJ.EF
{
  [Serializable]
  public partial class Computer : IIndexedForCaching
  {
    public string AuthLevel { get; set; }

    public string GetTellerNames()
    {
      return GetTellerNames(Teller1, Teller2);
    }

    public static string GetTellerNames(Guid? tellerGuid1, Guid? tellerGuid2)
    {
      var tellers = new TellerCacher().AllForThisElection;

      var tellersOnThisComputer = new List<Teller>
      {
        tellers.FirstOrDefault(t => t.TellerGuid == tellerGuid1),
        tellers.FirstOrDefault(t => t.TellerGuid == tellerGuid2)
      };
      return tellersOnThisComputer.Select(t => t == null ? "" : t.Name).JoinedAsString(", ", true);
    }
  }
}