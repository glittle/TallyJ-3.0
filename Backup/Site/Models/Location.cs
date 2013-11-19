using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class Location : Entity
    {
        public System.Guid ElectionGuid { get; set; }
        public System.Guid LocationGuid { get; set; }
        public string Name { get; set; }
        public string ContactInfo { get; set; }
        public string Long { get; set; }
        public string Lat { get; set; }
        public string TallyStatus { get; set; }
        public Nullable<int> SortOrder { get; set; }
        public Nullable<int> BallotsCollected { get; set; }
    }
}
