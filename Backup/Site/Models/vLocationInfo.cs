using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class vLocationInfo : Entity
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
        public string ComputerCode { get; set; }
        public Nullable<bool> IsSingleNameElection { get; set; }
        public Nullable<int> BallotsAtComputer { get; set; }
        public Nullable<int> BallotsAtLocation { get; set; }
        public Nullable<System.DateTime> LastContact { get; set; }
        public string TellerName { get; set; }
        public long SortOrder2 { get; set; }
    }
}
