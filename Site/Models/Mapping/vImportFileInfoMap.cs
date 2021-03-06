using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class vImportFileInfoMap : EntityTypeConfiguration<vImportFileInfo>
    {
        public vImportFileInfoMap()
        {
            // Primary Key
            this.HasKey(t => new { t.C_RowId, t.ElectionGuid });

            // Properties
            this.Property(t => t.C_RowId)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            this.Property(t => t.OriginalFileName)
                .HasMaxLength(50);

            this.Property(t => t.ProcessingStatus)
                .HasMaxLength(20);

            this.Property(t => t.FileType)
                .HasMaxLength(10);

            // Table & Column Mappings
            this.ToTable("vImportFileInfo", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.UploadTime).HasColumnName("UploadTime");
            this.Property(t => t.ImportTime).HasColumnName("ImportTime");
            this.Property(t => t.FileSize).HasColumnName("FileSize");
            this.Property(t => t.HasContent).HasColumnName("HasContent");
            this.Property(t => t.FirstDataRow).HasColumnName("FirstDataRow");
            this.Property(t => t.ColumnsToRead).HasColumnName("ColumnsToRead");
            this.Property(t => t.OriginalFileName).HasColumnName("OriginalFileName");
            this.Property(t => t.ProcessingStatus).HasColumnName("ProcessingStatus");
            this.Property(t => t.FileType).HasColumnName("FileType");
            this.Property(t => t.Messages).HasColumnName("Messages");
            this.Property(t => t.CodePage).HasColumnName("CodePage");
        }
    }
}
