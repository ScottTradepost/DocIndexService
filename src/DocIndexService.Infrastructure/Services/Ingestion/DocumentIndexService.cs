using System.Text.Json;
using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Core.Entities;
using DocIndexService.Core.Enums;
using DocIndexService.Core.Interfaces;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Infrastructure.Services.Ingestion;

public sealed class DocumentIndexService : IDocumentIndexService
{
    private readonly DocIndexDbContext _dbContext;
    private readonly IClock _clock;

    public DocumentIndexService(DocIndexDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task IndexAsync(
        Document document,
        TextExtractionResult extraction,
        IReadOnlyList<EmbeddedChunk> embeddedChunks,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var currentVersion = await _dbContext.DocumentVersionsSet
            .Where(x => x.DocumentId == document.Id)
            .MaxAsync(x => (int?)x.VersionNumber, cancellationToken) ?? 0;

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            VersionNumber = currentVersion + 1,
            Sha256 = document.Sha256,
            FileLastModifiedUtc = document.FileLastModifiedUtc,
            ExtractedTextPath = null,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        await _dbContext.DocumentVersionsSet.AddAsync(version, cancellationToken);

        var existingChunks = await _dbContext.DocumentChunksSet
            .Where(x => x.DocumentId == document.Id)
            .ToListAsync(cancellationToken);
        if (existingChunks.Count > 0)
        {
            _dbContext.DocumentChunksSet.RemoveRange(existingChunks);
        }

        foreach (var item in embeddedChunks)
        {
            var chunk = new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ChunkIndex = item.Chunk.ChunkIndex,
                PageStart = item.Chunk.PageStart,
                PageEnd = item.Chunk.PageEnd,
                Text = item.Chunk.Text,
                TokenCount = item.Chunk.TokenCount,
                Embedding = item.Vector.ToArray(),
                EmbeddingModel = item.EmbeddingModel,
                EmbeddingVersion = item.EmbeddingVersion,
                MetadataJson = JsonSerializer.Serialize(new { extraction.IsPlaceholder }),
                CreatedUtc = now,
                UpdatedUtc = now
            };

            await _dbContext.DocumentChunksSet.AddAsync(chunk, cancellationToken);
        }

        document.MimeType = extraction.MimeType;
        document.Status = DocumentStatus.Indexed;
        document.IsDeleted = false;
        document.LastIndexedUtc = now;
        document.UpdatedUtc = now;
    }

    public Task MarkDeletedAsync(Document document, CancellationToken cancellationToken)
    {
        document.IsDeleted = true;
        document.Status = DocumentStatus.Ignored;
        document.UpdatedUtc = _clock.UtcNow;
        return Task.CompletedTask;
    }
}
