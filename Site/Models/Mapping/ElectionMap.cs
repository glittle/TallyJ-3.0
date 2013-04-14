using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class ElectionMap : EntityTypeConfiguration<Election>
    {
        public ElectionMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
            this.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(150);

            this.Property(t => t.Convenor)
                .HasMaxLength(150);

            this.Property(t => t.ElectionType)
                .HasMaxLength(5);

            this.Property(t => t.ElectionMode)
                .HasMaxLength(1);

            this.Property(t => t.CanVote)
                .HasMaxLength(1);

            this.Property(t => t.CanReceive)
                .HasMaxLength(1);

            this.Property(t => t.TallyStatus)
                .HasMaxLength(15);

            this.Property(t => t.LinkedElectionKind)
                .HasMaxLength(2);

            this.Property(t => t.OwnerLoginId)
                .HasMaxLength(50);

            this.Property(t => t.ElectionPasscode)
                .HasMaxLength(50);

            this.Property(t => t.C_RowVersion)
                .IsRequired()
                .IsFixedLength()
                .HasMaxLength(8)
                .IsRowVersion();

            // Table & Column Mappings
            this.ToTable("Election", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.Name).HasColumnName("Name");
            this.Property(t => t.Convenor).HasColumnName("Convenor");
            this.Property(t => t.DateOfElection).HasColumnName("DateOfElection");
            this.Property(t => t.ElectionType).HasColumnName("ElectionType");
            this.Property(t => t.ElectionMode).HasColumnName("ElectionMode");
            this.Property(t => t.NumberToElect).HasColumnName("NumberToElect");
            this.Property(t => t.NumberExtra).HasColumnName("NumberExtra");
            this.Property(t => t.CanVote).HasColumnName("CanVote");
            this.Property(t => t.CanReceive).HasColumnName("CanReceive");
            this.Property(t => t.LastEnvNum).HasColumnName("LastEnvNum");
            this.Property(t => t.TallyStatus).HasColumnName("TallyStatus");
            this.Property(t => t.ShowFullReport).HasColumnName("ShowFullReport");
            this.Property(t => t.LinkedElectionGuid).HasColumnName("LinkedElectionGuid");
            this.Property(t => t.LinkedElectionKind).HasColumnName("LinkedElectionKind");
            this.Property(t => t.OwnerLoginId).HasColumnName("OwnerLoginId");
            this.Property(t => t.ElectionPasscode).HasColumnName("ElectionPasscode");
            this.Property(t => t.ListedForPublicAsOf).HasColumnName("ListedForPublicAsOf");
            this.Property(t => t.C_RowVersion).HasColumnName("_RowVersion");
            this.Property(t => t.ListForPublic).HasColumnName("ListForPublic");
            this.Property(t => t.ShowAsTest).HasColumnName("ShowAsTest");
        }
    }
}
