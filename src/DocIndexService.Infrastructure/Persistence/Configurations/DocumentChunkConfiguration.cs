using DocIndexService.Core.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;

namespace DocIndexService.Infrastructure.Persistence.Configurations;

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        var embeddingComparer = new ValueComparer<float[]?>(
            (left, right) =>
                left == right ||
                (left != null && right != null && left.SequenceEqual(right)),
            values =>
                values == null
                    ? 0
                    : values.Aggregate(17, (current, value) => current * 31 + value.GetHashCode()),
            values => values == null ? null : values.ToArray());

        builder.ToTable("DocumentChunks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text).IsRequired();
        builder.Property(x => x.EmbeddingModel).HasMaxLength(200);
        builder.Property(x => x.EmbeddingVersion).HasMaxLength(50);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.DocumentId, x.ChunkIndex }).IsUnique();
        builder.HasIndex(x => x.EmbeddingModel);

        // TODO: Finalize vector dimensions after embedding model contract is fixed.
        var embeddingProperty = builder.Property(x => x.Embedding)
            .HasConversion(new ValueConverter<float[]?, Vector?>(
                value => value == null ? null : new Vector(value),
                value => value == null ? null : value.ToArray()))
            .HasColumnType("vector(1536)");

        embeddingProperty.Metadata.SetValueComparer(embeddingComparer);
    }
}
