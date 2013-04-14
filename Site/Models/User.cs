using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TallyJ.Models
{
    public partial class User
    {
        public System.Guid ApplicationId { get; set; }
        [Key]
        public System.Guid UserId { get; set; }
        public string UserName { get; set; }
        public bool IsAnonymous { get; set; }
        public System.DateTime LastActivityDate { get; set; }
    }
}
