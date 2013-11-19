using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class C_LogMap : EntityTypeConfiguration<C_Log>
    {
        public C_LogMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
            this.Property(t => t.ComputerCode)
                .HasMaxLength(2);

            // Table & Column Mappings
            this.ToTable("_Log", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.AsOf).HasColumnName("AsOf");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.LocationGuid).HasColumnName("LocationGuid");
            this.Property(t => t.ComputerCode).HasColumnName("ComputerCode");
            this.Property(t => t.Details).HasColumnName("Details");
        }
    }
}
