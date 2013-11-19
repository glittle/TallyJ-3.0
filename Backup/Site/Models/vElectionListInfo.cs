using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class vElectionListInfo : Entity
    {
        public System.Guid ElectionGuid { get; set; }
        public string Name { get; set; }
        public Nullable<bool> ListForPublic { get; set; }
        public Nullable<System.DateTime> ListedForPublicAsOf { get; set; }
        public string ElectionPasscode { get; set; }
        public Nullable<System.DateTime> DateOfElection { get; set; }
        public string ElectionType { get; set; }
        public string ElectionMode { get; set; }
        public Nullable<bool> ShowAsTest { get; set; }
        public Nullable<int> NumVoters { get; set; }
    }
}
