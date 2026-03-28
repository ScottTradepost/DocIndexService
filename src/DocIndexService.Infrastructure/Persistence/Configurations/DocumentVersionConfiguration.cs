using DocIndexService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocIndexService.Infrastructure.Persistence.Configurations;

public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("DocumentVersions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ExtractedTextPath).HasMaxLength(1500);
        builder.Property(x => x.CreatedUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique();
        builder.HasIndex(x => x.Sha256);

        builder.HasOne(x => x.Document)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
