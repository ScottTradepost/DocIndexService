using DocIndexService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocIndexService.Infrastructure.Persistence.Configurations;

public sealed class ApiClientConfiguration : IEntityTypeConfiguration<ApiClient>
{
    public void Configure(EntityTypeBuilder<ApiClient> builder)
    {
        builder.ToTable("ApiClients");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ClientSecretHash).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.AllowedScopes).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.CreatedUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedUtc).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.ClientId).IsUnique();
        builder.HasIndex(x => x.IsEnabled);
    }
}
