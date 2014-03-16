using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.CoreModels;

namespace TallyJ.EF
{
  [Serializable]
  public class Computer
  {
    // create or recreate from environment
    public Guid ComputerGuid { get; set; }
    public Guid LocationGuid { get; set; }
    public Guid ElectionGuid { get; set; }

    // also stored in one user's session
    public string ComputerCode { get; set; }
    public string Teller1 { get; set; }
    public string Teller2 { get; set; }

    public DateTime? LastContact { get; set; }

    public string AuthLevel { get; set; }
    public string SessionId { get; set; }

    public string GetTellerNames()
    {
      return TellerModel.GetTellerNames(Teller1, Teller2);
    }

  }
}