using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class ComputerMap : EntityTypeConfiguration<Computer>
    {
        public ComputerMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
            this.Property(t => t.ComputerCode)
                .HasMaxLength(2);

            // Table & Column Mappings
            this.ToTable("Computer", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.LastContact).HasColumnName("LastContact");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.LocationGuid).HasColumnName("LocationGuid");
            this.Property(t => t.ComputerCode).HasColumnName("ComputerCode");
            this.Property(t => t.ComputerInternalCode).HasColumnName("ComputerInternalCode");
            this.Property(t => t.LastBallotNum).HasColumnName("LastBallotNum");
            this.Property(t => t.Teller1).HasColumnName("Teller1");
            this.Property(t => t.Teller2).HasColumnName("Teller2");
            this.Property(t => t.ShadowElectionGuid).HasColumnName("ShadowElectionGuid");
            this.Property(t => t.BrowserInfo).HasColumnName("BrowserInfo");
        }
    }
}
