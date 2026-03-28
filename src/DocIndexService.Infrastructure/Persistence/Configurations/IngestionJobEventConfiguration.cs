using DocIndexService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocIndexService.Infrastructure.Persistence.Configurations;

public sealed class IngestionJobEventConfiguration : IEntityTypeConfiguration<IngestionJobEvent>
{
    public void Configure(EntityTypeBuilder<IngestionJobEvent> builder)
    {
        builder.ToTable("IngestionJobEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000);
        builder.Property(x => x.DetailsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.IngestionJobId, x.CreatedUtc });
    }
}
