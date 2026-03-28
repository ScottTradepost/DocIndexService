using DocIndexService.Application.Abstractions.External;
using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Core.Entities;
using Microsoft.Extensions.Logging;

namespace DocIndexService.Infrastructure.Services.Ingestion;

public sealed class TextExtractionService : ITextExtractionService
{
    private readonly ITikaClient _tikaClient;
    private readonly ILogger<TextExtractionService> _logger;

    public TextExtractionService(
        ITikaClient tikaClient,
        ILogger<TextExtractionService> logger)
    {
        _tikaClient = tikaClient;
        _logger = logger;
    }

    public async Task<TextExtractionResult> ExtractAsync(Document document, CancellationToken cancellationToken)
    {
        if (!File.Exists(document.FullPath))
        {
            return new TextExtractionResult(string.Empty, ResolveMimeType(document.Extension), IsPlaceholder: true);
        }

        try
        {
            var extracted = await _tikaClient.ExtractTextAsync(document.FullPath, cancellationToken);
            if (!string.IsNullOrWhiteSpace(extracted))
            {
                return new TextExtractionResult(extracted, ResolveMimeType(document.Extension), IsPlaceholder: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tika extraction failed for {Path}; falling back to basic text read", document.FullPath);
        }

        try
        {
            var fallback = await File.ReadAllTextAsync(document.FullPath, cancellationToken);
            return new TextExtractionResult(fallback, ResolveMimeType(document.Extension), IsPlaceholder: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback text extraction failed for {Path}", document.FullPath);
            return new TextExtractionResult(string.Empty, ResolveMimeType(document.Extension), IsPlaceholder: true);
        }
    }

    private static string ResolveMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".json" => "application/json",
            ".csv" => "text/csv",
            ".xml" => "application/xml",
            ".html" => "text/html",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
