using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class ResultTieMap : EntityTypeConfiguration<ResultTie>
    {
        public ResultTieMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
            // Table & Column Mappings
            this.ToTable("ResultTie", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.TieBreakGroup).HasColumnName("TieBreakGroup");
            this.Property(t => t.TieBreakRequired).HasColumnName("TieBreakRequired");
            this.Property(t => t.NumToElect).HasColumnName("NumToElect");
            this.Property(t => t.NumInTie).HasColumnName("NumInTie");
            this.Property(t => t.IsResolved).HasColumnName("IsResolved");
        }
    }
}
