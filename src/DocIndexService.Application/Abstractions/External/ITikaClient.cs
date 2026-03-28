namespace DocIndexService.Application.Abstractions.External;

public interface ITikaClient
{
    Task<string> ExtractTextAsync(string fullPath, CancellationToken cancellationToken);
}
