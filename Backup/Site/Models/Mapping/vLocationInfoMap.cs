using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class vLocationInfoMap : EntityTypeConfiguration<vLocationInfo>
    {
        public vLocationInfoMap()
        {
            // Primary Key
            this.HasKey(t => new { t.C_RowId, t.ElectionGuid, t.LocationGuid, t.Name, t.SortOrder2 });

            // Properties
            this.Property(t => t.C_RowId)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            this.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(50);

            this.Property(t => t.ContactInfo)
                .HasMaxLength(250);

            this.Property(t => t.Long)
                .HasMaxLength(50);

            this.Property(t => t.Lat)
                .HasMaxLength(50);

            this.Property(t => t.TallyStatus)
                .HasMaxLength(15);

            this.Property(t => t.ComputerCode)
                .HasMaxLength(2);

            this.Property(t => t.TellerName)
                .HasMaxLength(102);

            this.Property(t => t.SortOrder2)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            // Table & Column Mappings
            this.ToTable("vLocationInfo", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.LocationGuid).HasColumnName("LocationGuid");
            this.Property(t => t.Name).HasColumnName("Name");
            this.Property(t => t.ContactInfo).HasColumnName("ContactInfo");
            this.Property(t => t.Long).HasColumnName("Long");
            this.Property(t => t.Lat).HasColumnName("Lat");
            this.Property(t => t.TallyStatus).HasColumnName("TallyStatus");
            this.Property(t => t.SortOrder).HasColumnName("SortOrder");
            this.Property(t => t.BallotsCollected).HasColumnName("BallotsCollected");
            this.Property(t => t.ComputerCode).HasColumnName("ComputerCode");
            this.Property(t => t.IsSingleNameElection).HasColumnName("IsSingleNameElection");
            this.Property(t => t.BallotsAtComputer).HasColumnName("BallotsAtComputer");
            this.Property(t => t.BallotsAtLocation).HasColumnName("BallotsAtLocation");
            this.Property(t => t.LastContact).HasColumnName("LastContact");
            this.Property(t => t.TellerName).HasColumnName("TellerName");
            this.Property(t => t.SortOrder2).HasColumnName("SortOrder2");
        }
    }
}
