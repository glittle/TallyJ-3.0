using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class ResultTie : Entity
    {
        public System.Guid ElectionGuid { get; set; }
        public int TieBreakGroup { get; set; }
        public Nullable<bool> TieBreakRequired { get; set; }
        public int NumToElect { get; set; }
        public int NumInTie { get; set; }
        public Nullable<bool> IsResolved { get; set; }
    }
}
