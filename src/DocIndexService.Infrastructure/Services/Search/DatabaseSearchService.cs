using DocIndexService.Application.Abstractions.Persistence;
using DocIndexService.Application.Abstractions.Search;
using DocIndexService.Contracts.Api.Search;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Infrastructure.Services.Search;

public sealed class DatabaseSearchService : ISearchService
{
    private const int MaxLimit = 100;
    private const int SnippetLength = 220;
    private readonly IDocIndexDbContext _dbContext;

    public DatabaseSearchService(IDocIndexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
        => SearchCoreAsync("keyword", request, cancellationToken);

    public Task<SearchResponse> SimilarAsync(SearchRequest request, CancellationToken cancellationToken)
        => SearchCoreAsync("similar", request, cancellationToken);

    public Task<SearchResponse> AskAsync(SearchRequest request, CancellationToken cancellationToken)
        => SearchCoreAsync("ask", request, cancellationToken);

    public Task<SearchResponse> SummarizeAsync(SearchRequest request, CancellationToken cancellationToken)
        => SearchCoreAsync("summarize", request, cancellationToken);

    public Task<SearchResponse> ExtractAsync(SearchRequest request, CancellationToken cancellationToken)
        => SearchCoreAsync("extract", request, cancellationToken);

    private async Task<SearchResponse> SearchCoreAsync(string mode, SearchRequest request, CancellationToken cancellationToken)
    {
        var query = request.Query?.Trim() ?? string.Empty;
        var offset = Math.Max(request.Offset, 0);
        var limit = Math.Clamp(request.Limit <= 0 ? 20 : request.Limit, 1, MaxLimit);

        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResponse(
                Mode: mode,
                Message: "Search query is empty.",
                Results: Array.Empty<SearchResultItem>(),
                UtcTimestamp: DateTime.UtcNow);
        }

        var normalizedQuery = query.ToLowerInvariant();

        var metadataMatches = await _dbContext.Documents
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .Where(d =>
                (d.Title != null && d.Title.ToLower().Contains(normalizedQuery)) ||
                d.FileName.ToLower().Contains(normalizedQuery) ||
                d.RelativePath.ToLower().Contains(normalizedQuery) ||
                (d.Summary != null && d.Summary.ToLower().Contains(normalizedQuery)))
            .Select(d => new
            {
                d.Id,
                d.Title,
                d.RelativePath,
                d.Summary
            })
            .Take(limit * 10)
            .ToListAsync(cancellationToken);

        var chunkMatches = await _dbContext.DocumentChunks
            .AsNoTracking()
            .Where(c => c.Text.ToLower().Contains(normalizedQuery))
            .Join(
                _dbContext.Documents.AsNoTracking().Where(d => !d.IsDeleted),
                chunk => chunk.DocumentId,
                document => document.Id,
                (chunk, document) => new
                {
                    document.Id,
                    document.Title,
                    document.RelativePath,
                    document.Summary,
                    chunk.Text,
                    chunk.PageStart,
                    chunk.PageEnd
                })
            .Take(limit * 50)
            .ToListAsync(cancellationToken);

        var ranked = new Dictionary<Guid, RankedDocument>();

        foreach (var metadataMatch in metadataMatches)
        {
            if (!ranked.TryGetValue(metadataMatch.Id, out var item))
            {
                item = new RankedDocument(metadataMatch.Id, metadataMatch.Title, metadataMatch.RelativePath, metadataMatch.Summary);
                ranked[item.DocumentId] = item;
            }

            item.Score += 2.0;
            item.Snippet ??= CreateSnippet(metadataMatch.Summary ?? metadataMatch.Title, normalizedQuery);
        }

        foreach (var chunkMatch in chunkMatches)
        {
            if (!ranked.TryGetValue(chunkMatch.Id, out var item))
            {
                item = new RankedDocument(chunkMatch.Id, chunkMatch.Title, chunkMatch.RelativePath, chunkMatch.Summary);
                ranked[item.DocumentId] = item;
            }

            item.Score += 1.0 + (0.25 * CountOccurrences(chunkMatch.Text, normalizedQuery));

            if (item.Snippet is null)
            {
                item.Snippet = CreateSnippet(chunkMatch.Text, normalizedQuery);
                item.PageStart = chunkMatch.PageStart;
                item.PageEnd = chunkMatch.PageEnd;
            }
        }

        var ordered = ranked.Values
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var page = ordered
            .Skip(offset)
            .Take(limit)
            .Select(x => new SearchResultItem(
                DocumentId: x.DocumentId,
                Title: x.Title,
                Path: x.Path,
                Score: Math.Round(x.Score, 3),
                Snippet: x.Snippet,
                Summary: mode == "summarize" ? (x.Summary ?? x.Snippet) : x.Summary,
                PageStart: mode == "extract" || mode == "ask" ? x.PageStart : null,
                PageEnd: mode == "extract" || mode == "ask" ? x.PageEnd : null))
            .ToArray();

        return new SearchResponse(
            Mode: mode,
            Message: CreateMessage(mode, query, page, ordered.Count),
            Results: page,
            UtcTimestamp: DateTime.UtcNow);
    }

    private static int CountOccurrences(string text, string normalizedQuery)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(normalizedQuery))
        {
            return 0;
        }

        var count = 0;
        var searchStart = 0;
        var normalizedText = text.ToLowerInvariant();

        while (searchStart < normalizedText.Length)
        {
            var index = normalizedText.IndexOf(normalizedQuery, searchStart, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            count++;
            searchStart = index + normalizedQuery.Length;
        }

        return count;
    }

    private static string? CreateSnippet(string? text, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalizedText = text.ToLowerInvariant();
        var hitIndex = normalizedText.IndexOf(normalizedQuery, StringComparison.Ordinal);

        if (hitIndex < 0)
        {
            return text.Length <= SnippetLength ? text : text[..SnippetLength] + "...";
        }

        var start = Math.Max(hitIndex - (SnippetLength / 2), 0);
        var length = Math.Min(SnippetLength, text.Length - start);
        var snippet = text.Substring(start, length).Trim();

        if (start > 0)
        {
            snippet = "..." + snippet;
        }

        if ((start + length) < text.Length)
        {
            snippet += "...";
        }

        return snippet;
    }

    private static string CreateMessage(string mode, string query, IReadOnlyCollection<SearchResultItem> results, int totalCount)
    {
        if (totalCount == 0)
        {
            return $"No matches found for '{query}'.";
        }

        return mode switch
        {
            "ask" => $"Found {totalCount} supporting result(s) for '{query}'. Review top snippets for answer grounding.",
            "similar" => $"Found {totalCount} lexical similarity match(es) for '{query}'.",
            "summarize" => $"Found {totalCount} result(s) for '{query}' with summary context.",
            "extract" => $"Found {totalCount} extractable result(s) for '{query}'.",
            _ => $"Found {totalCount} result(s) for '{query}'."
        };
    }

    private sealed class RankedDocument
    {
        public RankedDocument(Guid documentId, string? title, string path, string? summary)
        {
            DocumentId = documentId;
            Title = title;
            Path = path;
            Summary = summary;
        }

        public Guid DocumentId { get; }
        public string? Title { get; }
        public string Path { get; }
        public string? Summary { get; }
        public double Score { get; set; }
        public string? Snippet { get; set; }
        public int? PageStart { get; set; }
        public int? PageEnd { get; set; }
    }
}