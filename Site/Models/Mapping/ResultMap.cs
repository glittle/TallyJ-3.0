using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace TallyJ.Models.Mapping
{
    public class ResultMap : EntityTypeConfiguration<Result>
    {
        public ResultMap()
        {
            // Primary Key
            this.HasKey(t => t.C_RowId);

            // Properties
            this.Property(t => t.Section)
                .IsRequired()
                .IsFixedLength()
                .HasMaxLength(1);

            // Table & Column Mappings
            this.ToTable("Result", "tj");
            this.Property(t => t.C_RowId).HasColumnName("_RowId");
            this.Property(t => t.ElectionGuid).HasColumnName("ElectionGuid");
            this.Property(t => t.PersonGuid).HasColumnName("PersonGuid");
            this.Property(t => t.VoteCount).HasColumnName("VoteCount");
            this.Property(t => t.Rank).HasColumnName("Rank");
            this.Property(t => t.Section).HasColumnName("Section");
            this.Property(t => t.CloseToPrev).HasColumnName("CloseToPrev");
            this.Property(t => t.CloseToNext).HasColumnName("CloseToNext");
            this.Property(t => t.IsTied).HasColumnName("IsTied");
            this.Property(t => t.TieBreakGroup).HasColumnName("TieBreakGroup");
            this.Property(t => t.TieBreakRequired).HasColumnName("TieBreakRequired");
            this.Property(t => t.TieBreakCount).HasColumnName("TieBreakCount");
            this.Property(t => t.IsTieResolved).HasColumnName("IsTieResolved");
            this.Property(t => t.RankInExtra).HasColumnName("RankInExtra");
            this.Property(t => t.ForceShowInOther).HasColumnName("ForceShowInOther");
        }
    }
}
