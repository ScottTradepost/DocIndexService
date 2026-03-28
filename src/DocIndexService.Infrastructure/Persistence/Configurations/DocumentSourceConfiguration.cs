using DocIndexService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocIndexService.Infrastructure.Persistence.Configurations;

public sealed class DocumentSourceConfiguration : IEntityTypeConfiguration<DocumentSource>
{
    public void Configure(EntityTypeBuilder<DocumentSource> builder)
    {
        builder.ToTable("DocumentSources");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RootPath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.IncludePatterns).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ExcludePatterns).HasMaxLength(500);
        builder.Property(x => x.CreatedUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.RootPath);
        builder.HasIndex(x => x.IsEnabled);

        builder.HasMany(x => x.Documents)
            .WithOne(x => x.Source)
            .HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.IngestionJobs)
            .WithOne(x => x.Source)
            .HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
