using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class C_Log : Entity
    {
        public System.DateTime AsOf { get; set; }
        public System.Guid ElectionGuid { get; set; }
        public Nullable<System.Guid> LocationGuid { get; set; }
        public string ComputerCode { get; set; }
        public string Details { get; set; }
    }
}
