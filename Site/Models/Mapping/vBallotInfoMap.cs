using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class vBallotInfoMap : EntityTypeConfiguration<vBallotInfo>
    {
        public vBallotInfoMap()
        {
            // Primary Key
            this.HasKey(t => new { t.C_RowId, t.LocationGuid, t.BallotGuid, t.StatusCode, t.ComputerCode, t.BallotNumAtComputer, t.C_RowVersion, t.ElectionGuid, t.LocationId, t.LocationName });

            // Properties
            this.Property(t => t.C_RowId)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            this.Property(t => t.StatusCode)
                .IsRequired()
                .HasMaxLength(10);

            this.Property(t => t.ComputerCode)
                .IsRequired()
                .HasMaxLength(2);

            this.Property(t => t.BallotNumAtComputer)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            this.Property(t => t.C_BallotCode)
                .HasMaxLength(32);

            this.Property(t => t.C_RowVersion)
                .IsRequired()
                .IsFixedLength()
                .HasMaxLength(8)
                .IsRowVersion()
                .IsConcurrencyToken(false);

            this.Property(t => t.LocationId)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            this.Property(t => t.LocationName)
                .IsRequired()
                .HasMaxLength(50);

            this.Property(t => t.TallyStatus)
                .HasMaxLength(15);

            this.Property(t => t.TellerAtKeyboardName)
                .HasMaxLength(50);

            this.Property(t => t.TellerAssistingName)
                .HasMaxLength(50);

            // Table & Column Mappings
            this.ToTable("vBallotInfo", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.LocationGuid).HasColumnName("LocationGuid");
            this.Property(t => t.BallotGuid).HasColumnName("BallotGuid");
            this.Property(t => t.StatusCode).HasColumnName("StatusCode");
            this.Property(t => t.ComputerCode).HasColumnName("ComputerCode");
            this.Property(t => t.BallotNumAtComputer).HasColumnName("BallotNumAtComputer");
            this.Property(t => t.C_BallotCode).HasColumnName("_BallotCode");
            this.Property(t => t.TellerAtKeyboard).HasColumnName("TellerAtKeyboard");
            this.Property(t => t.TellerAssisting).HasColumnName("TellerAssisting");
            this.Property(t => t.C_RowVersion).HasColumnName("_RowVersion");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.LocationId).HasColumnName("LocationId");
            this.Property(t => t.LocationName).HasColumnName("LocationName");
            this.Property(t => t.LocationSortOrder).HasColumnName("LocationSortOrder");
            this.Property(t => t.TallyStatus).HasColumnName("TallyStatus");
            this.Property(t => t.TellerAtKeyboardName).HasColumnName("TellerAtKeyboardName");
            this.Property(t => t.TellerAssistingName).HasColumnName("TellerAssistingName");
            this.Property(t => t.RowVersionInt).HasColumnName("RowVersionInt");
            this.Property(t => t.SpoiledCount).HasColumnName("SpoiledCount");
            this.Property(t => t.VotesChanged).HasColumnName("VotesChanged");
        }
    }
}
