using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class VoteMap : EntityTypeConfiguration<Vote>
    {
        public VoteMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
            this.Property(t => t.StatusCode)
                .IsRequired()
                .HasMaxLength(10);

            this.Property(t => t.C_RowVersion)
                .IsRequired()
                .IsFixedLength()
                .HasMaxLength(8)
                .IsRowVersion();

            // Table & Column Mappings
            this.ToTable("Vote", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.BallotGuid).HasColumnName("BallotGuid");
            this.Property(t => t.PositionOnBallot).HasColumnName("PositionOnBallot");
            this.Property(t => t.PersonGuid).HasColumnName("PersonGuid");
            this.Property(t => t.StatusCode).HasColumnName("StatusCode");
            this.Property(t => t.InvalidReasonGuid).HasColumnName("InvalidReasonGuid");
            this.Property(t => t.SingleNameElectionCount).HasColumnName("SingleNameElectionCount");
            this.Property(t => t.C_RowVersion).HasColumnName("_RowVersion");
            this.Property(t => t.PersonCombinedInfo).HasColumnName("PersonCombinedInfo");
        }
    }
}
