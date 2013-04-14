using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class JoinElectionUser : Entity
    {
        public System.Guid ElectionGuid { get; set; }
        public System.Guid UserId { get; set; }
        public string Role { get; set; }
    }
}
