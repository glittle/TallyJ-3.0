using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class ResultSummaryMap : EntityTypeConfiguration<ResultSummary>
    {
        public ResultSummaryMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
            this.Property(t => t.ResultType)
                .IsRequired()
                .IsFixedLength()
                .HasMaxLength(1);

            // Table & Column Mappings
            this.ToTable("ResultSummary", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.ResultType).HasColumnName("ResultType");
            this.Property(t => t.UseOnReports).HasColumnName("UseOnReports");
            this.Property(t => t.NumVoters).HasColumnName("NumVoters");
            this.Property(t => t.NumEligibleToVote).HasColumnName("NumEligibleToVote");
            this.Property(t => t.MailedInBallots).HasColumnName("MailedInBallots");
            this.Property(t => t.DroppedOffBallots).HasColumnName("DroppedOffBallots");
            this.Property(t => t.InPersonBallots).HasColumnName("InPersonBallots");
            this.Property(t => t.SpoiledBallots).HasColumnName("SpoiledBallots");
            this.Property(t => t.SpoiledVotes).HasColumnName("SpoiledVotes");
            this.Property(t => t.TotalVotes).HasColumnName("TotalVotes");
            this.Property(t => t.BallotsReceived).HasColumnName("BallotsReceived");
            this.Property(t => t.BallotsNeedingReview).HasColumnName("BallotsNeedingReview");
            this.Property(t => t.CalledInBallots).HasColumnName("CalledInBallots");
        }
    }
}
