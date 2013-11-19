using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class LocationMap : EntityTypeConfiguration<Location>
    {
        public LocationMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
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

            // Table & Column Mappings
            this.ToTable("Location", "tj");
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
        }
    }
}
