namespace DocIndexService.Core.Options;

public sealed class TikaOptions
{
    public const string SectionName = "Tika";

    public string BaseUrl { get; init; } = "http://localhost:9998";
    public int TimeoutSeconds { get; init; } = 30;
}
