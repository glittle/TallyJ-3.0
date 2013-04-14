using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class Computer : Entity
    {
        public Nullable<System.DateTime> LastContact { get; set; }
        public Nullable<System.Guid> ElectionGuid { get; set; }
        public Nullable<System.Guid> LocationGuid { get; set; }
        public string ComputerCode { get; set; }
        public Nullable<int> ComputerInternalCode { get; set; }
        public Nullable<int> LastBallotNum { get; set; }
        public Nullable<System.Guid> Teller1 { get; set; }
        public Nullable<System.Guid> Teller2 { get; set; }
        public Nullable<System.Guid> ShadowElectionGuid { get; set; }
        public string BrowserInfo { get; set; }
    }
}
