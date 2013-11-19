using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class Teller : Entity
    {
        public System.Guid ElectionGuid { get; set; }
        public System.Guid TellerGuid { get; set; }
        public string Name { get; set; }
        public string UsingComputerCode { get; set; }
        public Nullable<bool> IsHeadTeller { get; set; }
        public byte[] C_RowVersion { get; set; }
    }
}
