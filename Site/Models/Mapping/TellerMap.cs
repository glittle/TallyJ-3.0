using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class TellerMap : EntityTypeConfiguration<Teller>
    {
        public TellerMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
            this.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(50);

            this.Property(t => t.UsingComputerCode)
                .HasMaxLength(2);

            this.Property(t => t.C_RowVersion)
                .IsRequired()
                .IsFixedLength()
                .HasMaxLength(8)
                .IsRowVersion();

            // Table & Column Mappings
            this.ToTable("Teller", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.TellerGuid).HasColumnName("TellerGuid");
            this.Property(t => t.Name).HasColumnName("Name");
            this.Property(t => t.UsingComputerCode).HasColumnName("UsingComputerCode");
            this.Property(t => t.IsHeadTeller).HasColumnName("IsHeadTeller");
            this.Property(t => t.C_RowVersion).HasColumnName("_RowVersion");
        }
    }
}
