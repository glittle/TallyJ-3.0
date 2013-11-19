using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class PersonMap : EntityTypeConfiguration<Person>
    {
        public PersonMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
            this.Property(t => t.LastName)
                .IsRequired()
                .HasMaxLength(50);

            this.Property(t => t.FirstName)
                .HasMaxLength(50);

            this.Property(t => t.OtherLastNames)
                .HasMaxLength(100);

            this.Property(t => t.OtherNames)
                .HasMaxLength(100);

            this.Property(t => t.OtherInfo)
                .HasMaxLength(150);

            this.Property(t => t.Area)
                .HasMaxLength(50);

            this.Property(t => t.BahaiId)
                .HasMaxLength(20);

            this.Property(t => t.AgeGroup)
                .HasMaxLength(2);

            this.Property(t => t.VotingMethod)
                .HasMaxLength(1);

            this.Property(t => t.C_RowVersion)
                .IsRequired()
                .IsFixedLength()
                .HasMaxLength(8)
                .IsRowVersion()
                .IsConcurrencyToken(false);

            this.Property(t => t.C_RowVersionInt)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);

            this.Property(t => t.C_FullName)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed)
                .HasMaxLength(461);

            this.Property(t => t.C_FullNameFL)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed)
                .HasMaxLength(460);

            // Table & Column Mappings
            this.ToTable("Person", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.PersonGuid).HasColumnName("PersonGuid");
            this.Property(t => t.LastName).HasColumnName("LastName");
            this.Property(t => t.FirstName).HasColumnName("FirstName");
            this.Property(t => t.OtherLastNames).HasColumnName("OtherLastNames");
            this.Property(t => t.OtherNames).HasColumnName("OtherNames");
            this.Property(t => t.OtherInfo).HasColumnName("OtherInfo");
            this.Property(t => t.Area).HasColumnName("Area");
            this.Property(t => t.BahaiId).HasColumnName("BahaiId");
            this.Property(t => t.CombinedInfo).HasColumnName("CombinedInfo");
            this.Property(t => t.CombinedSoundCodes).HasColumnName("CombinedSoundCodes");
            this.Property(t => t.CombinedInfoAtStart).HasColumnName("CombinedInfoAtStart");
            this.Property(t => t.AgeGroup).HasColumnName("AgeGroup");
            this.Property(t => t.CanVote).HasColumnName("CanVote");
            this.Property(t => t.CanReceiveVotes).HasColumnName("CanReceiveVotes");
            this.Property(t => t.IneligibleReasonGuid).HasColumnName("IneligibleReasonGuid");
            this.Property(t => t.RegistrationTime).HasColumnName("RegistrationTime");
            this.Property(t => t.VotingLocationGuid).HasColumnName("VotingLocationGuid");
            this.Property(t => t.VotingMethod).HasColumnName("VotingMethod");
            this.Property(t => t.EnvNum).HasColumnName("EnvNum");
            this.Property(t => t.C_RowVersion).HasColumnName("_RowVersion");
            this.Property(t => t.C_FullName).HasColumnName("_FullName");
            this.Property(t => t.C_RowVersionInt).HasColumnName("_RowVersionInt");
            this.Property(t => t.C_FullNameFL).HasColumnName("_FullNameFL");
            this.Property(t => t.TellerAtKeyboard).HasColumnName("TellerAtKeyboard");
            this.Property(t => t.TellerAssisting).HasColumnName("TellerAssisting");
        }
    }
}
