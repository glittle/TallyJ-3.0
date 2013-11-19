using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class vElectionListInfoMap : EntityTypeConfiguration<vElectionListInfo>
    {
        public vElectionListInfoMap()
        {
            // Primary Key
            this.HasKey(t => new { t.C_RowId, t.ElectionGuid, t.Name });

            // Properties
            this.Property(t => t.C_RowId)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            this.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(150);

            this.Property(t => t.ElectionPasscode)
                .HasMaxLength(50);

            this.Property(t => t.ElectionType)
                .HasMaxLength(5);

            this.Property(t => t.ElectionMode)
                .HasMaxLength(1);

            // Table & Column Mappings
            this.ToTable("vElectionListInfo", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.Name).HasColumnName("Name");
            this.Property(t => t.ListForPublic).HasColumnName("ListForPublic");
            this.Property(t => t.ListedForPublicAsOf).HasColumnName("ListedForPublicAsOf");
            this.Property(t => t.ElectionPasscode).HasColumnName("ElectionPasscode");
            this.Property(t => t.DateOfElection).HasColumnName("DateOfElection");
            this.Property(t => t.ElectionType).HasColumnName("ElectionType");
            this.Property(t => t.ElectionMode).HasColumnName("ElectionMode");
            this.Property(t => t.ShowAsTest).HasColumnName("ShowAsTest");
            this.Property(t => t.NumVoters).HasColumnName("NumVoters");
        }
    }
}
