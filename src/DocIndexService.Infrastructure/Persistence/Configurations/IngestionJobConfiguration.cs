using DocIndexService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocIndexService.Infrastructure.Persistence.Configurations;

public sealed class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        builder.ToTable("IngestionJobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobType).HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.SourceId);
        builder.HasIndex(x => x.DocumentId);
        builder.HasIndex(x => new { x.Status, x.StartedUtc });

        builder.HasOne(x => x.Source)
            .WithMany(x => x.IngestionJobs)
            .HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Document)
            .WithMany(x => x.IngestionJobs)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Events)
            .WithOne(x => x.IngestionJob)
            .HasForeignKey(x => x.IngestionJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
