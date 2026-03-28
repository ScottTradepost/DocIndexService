using System.Security.Cryptography;
using DocIndexService.Application.Abstractions.Ingestion;

namespace DocIndexService.Infrastructure.Services.Ingestion;

public sealed class FileFingerprintService : IFileFingerprintService
{
    public async Task<FileFingerprintResult> ComputeAsync(string fullPath, CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(fullPath);

        await using var fileStream = File.OpenRead(fullPath);
        using var hasher = SHA256.Create();
        var hashBytes = await hasher.ComputeHashAsync(fileStream, cancellationToken);

        return new FileFingerprintResult(
            Convert.ToHexString(hashBytes).ToLowerInvariant(),
            fileInfo.LastWriteTimeUtc,
            fileInfo.Length);
    }
}
