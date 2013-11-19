using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace TallyJ.Models
{
    /// <summary>
    /// All entities need to derive from this, to be found automatically
    /// </summary>
    [Serializable]
    public abstract class BaseEntity
    {
    }

    /// <summary>
    /// All tables in general use and updateable should have _RowId
    /// </summary>
    [Serializable]
    public abstract class Entity : BaseEntity
    {
        [Key]
        public virtual int C_RowId { get; set; }
    }
}