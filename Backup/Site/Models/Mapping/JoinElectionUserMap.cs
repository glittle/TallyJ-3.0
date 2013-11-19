using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class JoinElectionUserMap : EntityTypeConfiguration<JoinElectionUser>
    {
        public JoinElectionUserMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
            this.Property(t => t.Role)
                .HasMaxLength(10);

            // Table & Column Mappings
            this.ToTable("JoinElectionUser", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.UserId).HasColumnName("UserId");
            this.Property(t => t.Role).HasColumnName("Role");

        }
    }
}
