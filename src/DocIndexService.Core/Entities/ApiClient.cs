namespace DocIndexService.Core.Entities;

public sealed class ApiClient
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecretHash { get; set; } = string.Empty;
    public string AllowedScopes { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastUsedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
