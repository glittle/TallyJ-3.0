using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class Message : Entity
    {
        public System.Guid ElectionGuid { get; set; }
        public string Title { get; set; }
        public string Details { get; set; }
        public byte[] C_RowVersion { get; set; }
        public System.DateTime AsOf { get; set; }
    }
}
