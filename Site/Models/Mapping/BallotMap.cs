using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class BallotMap : EntityTypeConfiguration<Ballot>
    {
        public BallotMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
            this.Property(t => t.StatusCode)
                .IsRequired()
                .HasMaxLength(10);

            this.Property(t => t.ComputerCode)
                .IsRequired()
                .HasMaxLength(2);

            this.Property(t => t.C_BallotCode)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed)
                .HasMaxLength(32);

            this.Property(t => t.C_RowVersion)
                .IsRequired()
                .IsFixedLength()
                .HasMaxLength(8)
                .IsRowVersion()
                .IsConcurrencyToken(false);

            // Table & Column Mappings
            this.ToTable("Ballot", "tj");
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
        }
    }
}
