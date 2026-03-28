using System.Text.RegularExpressions;
using DocIndexService.Application.Abstractions.Ingestion;

namespace DocIndexService.Infrastructure.Services.Ingestion;

public sealed class ChunkingService : IChunkingService
{
    private static readonly Regex ParagraphSplitRegex = new("(\\r?\\n){2,}", RegexOptions.Compiled);

    public Task<IReadOnlyList<TextChunk>> ChunkAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult<IReadOnlyList<TextChunk>>(Array.Empty<TextChunk>());
        }

        var paragraphs = ParagraphSplitRegex
            .Split(text)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();

        var chunks = new List<TextChunk>();
        var index = 0;

        foreach (var paragraph in paragraphs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            const int maxChunkLength = 1200;
            if (paragraph.Length <= maxChunkLength)
            {
                chunks.Add(new TextChunk(index++, paragraph, EstimateTokenCount(paragraph)));
                continue;
            }

            for (var start = 0; start < paragraph.Length; start += maxChunkLength)
            {
                var length = Math.Min(maxChunkLength, paragraph.Length - start);
                var textSlice = paragraph.Substring(start, length);
                chunks.Add(new TextChunk(index++, textSlice, EstimateTokenCount(textSlice)));
            }
        }

        return Task.FromResult<IReadOnlyList<TextChunk>>(chunks);
    }

    private static int EstimateTokenCount(string text)
    {
        return Math.Max(1, text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
    }
}
