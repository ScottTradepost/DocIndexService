using System.ComponentModel.DataAnnotations;

namespace DocIndexService.Core.Options;

public sealed class PostgresOptions
{
    public const string SectionName = "Postgres";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    public string MigrationsAssembly { get; init; } = string.Empty;
}
