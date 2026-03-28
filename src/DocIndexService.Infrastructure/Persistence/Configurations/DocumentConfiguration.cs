using DocIndexService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocIndexService.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RelativePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.FullPath).HasMaxLength(1500).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Extension).HasMaxLength(50).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(200);
        builder.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.SourceId, x.RelativePath }).IsUnique();
        builder.HasIndex(x => x.Sha256);
        builder.HasIndex(x => new { x.Status, x.IsDeleted });

        builder.HasMany(x => x.Chunks)
            .WithOne(x => x.Document)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Versions)
            .WithOne(x => x.Document)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.IngestionJobs)
            .WithOne(x => x.Document)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
