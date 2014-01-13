using System;
using System.Collections.Generic;
using System.Linq;
using TallyJ.Code;
using TallyJ.CoreModels;

namespace TallyJ.EF
{
  [Serializable]
  public partial class Computer : IIndexedForCaching
  {
    // create or recreate from environment
    public int C_RowId { get; set; }
    public Guid LocationGuid { get; set; }

    // also stored in one user's session
    public string ComputerCode { get; set; }
    public string Teller1 { get; set; }
    public string Teller2 { get; set; }

    public DateTime? LastContact { get; set; }

    public string TempAuthLevel { get; set; }
    public string TempSessionId { get; set; }

    public string GetTellerNames()
    {
      return TellerModel.GetTellerNames(Teller1, Teller2);
    }

  }
}