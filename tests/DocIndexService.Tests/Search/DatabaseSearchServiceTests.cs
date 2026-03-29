using DocIndexService.Contracts.Api.Search;
using DocIndexService.Core.Entities;
using DocIndexService.Infrastructure.Persistence;
using DocIndexService.Infrastructure.Services.Search;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Tests.Search;

public sealed class DatabaseSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_ShouldReturnRankedChunkMatches()
    {
        await using var db = CreateDbContext();
        var source = await SeedSourceAsync(db);
        var document = await SeedDocumentAsync(db, source.Id, "policies/hr-handbook.txt", "HR Handbook");

        await db.DocumentChunksSet.AddAsync(new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            ChunkIndex = 0,
            Text = "Remote work policy requires manager approval and annual acknowledgement.",
            PageStart = 1,
            PageEnd = 1,
            TokenCount = 12,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new DatabaseSearchService(db);
        var response = await service.SearchAsync(new SearchRequest("manager approval", 10, 0), CancellationToken.None);

        Assert.Equal("keyword", response.Mode);
        Assert.NotEmpty(response.Results);

        var topResult = response.Results.First();
        Assert.Equal(document.Id, topResult.DocumentId);
        Assert.Contains("policies/hr-handbook.txt", topResult.Path, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("manager approval", topResult.Snippet ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SummarizeAsync_ShouldUseDocumentSummaryWhenAvailable()
    {
        await using var db = CreateDbContext();
        var source = await SeedSourceAsync(db);
        var document = await SeedDocumentAsync(
            db,
            source.Id,
            "finance/q2-report.txt",
            "Q2 Report",
            summary: "Q2 report highlights a 12% growth in recurring revenue.");

        await db.DocumentChunksSet.AddAsync(new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            ChunkIndex = 0,
            Text = "The quarter ended with 12 percent recurring revenue growth and improved margins.",
            TokenCount = 14,
            PageStart = 2,
            PageEnd = 2,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new DatabaseSearchService(db);
        var response = await service.SummarizeAsync(new SearchRequest("recurring revenue", 10, 0), CancellationToken.None);

        Assert.Equal("summarize", response.Mode);
        var result = Assert.Single(response.Results);
        Assert.Equal(document.Id, result.DocumentId);
        Assert.Equal("Q2 report highlights a 12% growth in recurring revenue.", result.Summary);
    }

    private static DocIndexDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DocIndexDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new DocIndexDbContext(options);
    }

    private static async Task<DocumentSource> SeedSourceAsync(DocIndexDbContext db)
    {
        var now = DateTime.UtcNow;
        var source = new DocumentSource
        {
            Id = Guid.NewGuid(),
            Name = "Test Source",
            RootPath = "C:/docs",
            IsEnabled = true,
            IsRecursive = true,
            IncludePatterns = "*.txt",
            ExcludePatterns = string.Empty,
            ScanIntervalMinutes = 15,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        await db.DocumentSourcesSet.AddAsync(source);
        await db.SaveChangesAsync();
        return source;
    }

    private static async Task<Document> SeedDocumentAsync(
        DocIndexDbContext db,
        Guid sourceId,
        string relativePath,
        string title,
        string? summary = null)
    {
        var now = DateTime.UtcNow;
        var document = new Document
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            RelativePath = relativePath,
            FullPath = $"C:/docs/{relativePath}",
            FileName = Path.GetFileName(relativePath),
            Extension = Path.GetExtension(relativePath),
            Sha256 = "hash",
            FileSize = 100,
            FileLastModifiedUtc = now,
            Title = title,
            Summary = summary,
            LastIndexedUtc = now,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        await db.DocumentsSet.AddAsync(document);
        await db.SaveChangesAsync();
        return document;
    }
}
