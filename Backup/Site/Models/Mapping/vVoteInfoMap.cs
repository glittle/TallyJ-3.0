using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class vVoteInfoMap : EntityTypeConfiguration<vVoteInfo>
    {
        public vVoteInfoMap()
        {
            // Primary Key
            this.HasKey(t => new { t.VoteId, t.VoteStatusCode, t.PositionOnBallot, t.BallotGuid, t.BallotId, t.BallotStatusCode, t.LocationId, t.ElectionGuid });

            // Properties
            this.Property(t => t.VoteId)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            this.Property(t => t.VoteStatusCode)
                .IsRequired()
                .HasMaxLength(10);

            this.Property(t => t.PositionOnBallot)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            this.Property(t => t.PersonFullName)
                .HasMaxLength(461);

            this.Property(t => t.PersonFullNameFL)
                .HasMaxLength(460);

            this.Property(t => t.BallotId)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            this.Property(t => t.BallotStatusCode)
                .IsRequired()
                .HasMaxLength(10);

            this.Property(t => t.C_BallotCode)
                .HasMaxLength(32);

            this.Property(t => t.LocationId)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            this.Property(t => t.LocationTallyStatus)
                .HasMaxLength(15);

            // Table & Column Mappings
            this.ToTable("vVoteInfo", "tj");
            this.Property(t => t.VoteId).HasColumnName("VoteId");
            this.Property(t => t.VoteStatusCode).HasColumnName("VoteStatusCode");
            this.Property(t => t.SingleNameElectionCount).HasColumnName("SingleNameElectionCount");
            this.Property(t => t.IsSingleNameElection).HasColumnName("IsSingleNameElection");
            this.Property(t => t.PositionOnBallot).HasColumnName("PositionOnBallot");
            this.Property(t => t.VoteIneligibleReasonGuid).HasColumnName("VoteIneligibleReasonGuid");
            this.Property(t => t.PersonCombinedInfoInVote).HasColumnName("PersonCombinedInfoInVote");
            this.Property(t => t.PersonCombinedInfo).HasColumnName("PersonCombinedInfo");
            this.Property(t => t.PersonGuid).HasColumnName("PersonGuid");
            this.Property(t => t.PersonId).HasColumnName("PersonId");
            this.Property(t => t.PersonFullName).HasColumnName("PersonFullName");
            this.Property(t => t.PersonFullNameFL).HasColumnName("PersonFullNameFL");
            this.Property(t => t.CanReceiveVotes).HasColumnName("CanReceiveVotes");
            this.Property(t => t.PersonIneligibleReasonGuid).HasColumnName("PersonIneligibleReasonGuid");
            this.Property(t => t.ResultId).HasColumnName("ResultId");
            this.Property(t => t.BallotGuid).HasColumnName("BallotGuid");
            this.Property(t => t.BallotId).HasColumnName("BallotId");
            this.Property(t => t.BallotStatusCode).HasColumnName("BallotStatusCode");
            this.Property(t => t.C_BallotCode).HasColumnName("_BallotCode");
            this.Property(t => t.LocationId).HasColumnName("LocationId");
            this.Property(t => t.LocationTallyStatus).HasColumnName("LocationTallyStatus");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
        }
    }
}
