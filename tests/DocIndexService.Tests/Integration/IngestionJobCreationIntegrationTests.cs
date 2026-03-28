using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Core.Entities;
using DocIndexService.Core.Enums;
using DocIndexService.Core.Interfaces;
using DocIndexService.Core.Options;
using DocIndexService.Infrastructure.Persistence;
using DocIndexService.Infrastructure.Services.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocIndexService.Tests.Integration;

public sealed class IngestionJobCreationIntegrationTests
{
    [Fact]
    public async Task TriggerScanAsync_ShouldCreatePendingDocumentJob_ForNewFile()
    {
        await using var db = CreateDbContext();
        var now = DateTime.UtcNow;

        var source = new DocumentSource
        {
            Id = Guid.NewGuid(),
            Name = "Source-Scan",
            RootPath = "C:/scan-root",
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

        var scannedFile = new ScannedFileEntry(
            RelativePath: "docs/readme.txt",
            FullPath: "C:/scan-root/docs/readme.txt",
            LastModifiedUtc: now,
            FileSize: 32,
            Extension: ".txt");

        var coordinator = CreateCoordinator(db, new SingleFileScanner(scannedFile));

        await coordinator.TriggerScanAsync(source.Id, fullReconciliation: false, cancellationToken: CancellationToken.None);

        var scanJob = await db.IngestionJobsSet
            .Where(j => j.SourceId == source.Id && (j.JobType == IngestionJobType.ScanIncremental || j.JobType == IngestionJobType.ScanFullReconciliation))
            .OrderByDescending(j => j.CreatedUtc)
            .FirstOrDefaultAsync();

        Assert.NotNull(scanJob);
        Assert.Equal(IngestionJobStatus.Succeeded, scanJob!.Status);

        var queuedDocumentJob = await db.IngestionJobsSet
            .Where(j => j.SourceId == source.Id && j.JobType == IngestionJobType.ReprocessDocument)
            .OrderByDescending(j => j.CreatedUtc)
            .FirstOrDefaultAsync();

        Assert.NotNull(queuedDocumentJob);
        Assert.Equal(IngestionJobStatus.Pending, queuedDocumentJob!.Status);
        Assert.NotNull(queuedDocumentJob.DocumentId);

        var jobEventTypes = await db.IngestionJobEventsSet
            .Where(e => e.IngestionJobId == queuedDocumentJob.Id)
            .Select(e => e.EventType)
            .ToListAsync();

        Assert.Contains("ChangeDetected", jobEventTypes);

        var savedDocument = await db.DocumentsSet.FirstOrDefaultAsync(d => d.SourceId == source.Id && d.RelativePath == "docs/readme.txt");
        Assert.NotNull(savedDocument);
        Assert.Equal(DocumentStatus.Pending, savedDocument!.Status);
    }

    private static DocIndexDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DocIndexDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new DocIndexDbContext(options);
    }

    private static IngestionCoordinator CreateCoordinator(DocIndexDbContext db, IFileScanner scanner)
    {
        return new IngestionCoordinator(
            db,
            scanner,
            new FixedFingerprintService(),
            new NoOpExtractionService(),
            new NoOpChunkingService(),
            new NoOpEmbeddingService(),
            new NoOpDocumentIndexService(),
            new TestClock(),
            Options.Create(new ScanOptions()),
            NullLogger<IngestionCoordinator>.Instance);
    }

    private sealed class SingleFileScanner : IFileScanner
    {
        private readonly ScannedFileEntry _entry;

        public SingleFileScanner(ScannedFileEntry entry)
        {
            _entry = entry;
        }

        public Task<SourceScanSnapshot> ScanAsync(DocumentSource source, bool fullReconciliation, CancellationToken cancellationToken)
        {
            var snapshot = new SourceScanSnapshot(
                source.Id,
                source.RootPath,
                DateTime.UtcNow,
                new[] { _entry },
                fullReconciliation);

            return Task.FromResult(snapshot);
        }
    }

    private sealed class FixedFingerprintService : IFileFingerprintService
    {
        public Task<FileFingerprintResult> ComputeAsync(string fullPath, CancellationToken cancellationToken)
        {
            return Task.FromResult(new FileFingerprintResult("sha-256-fixed", DateTime.UtcNow, 32));
        }
    }

    private sealed class NoOpExtractionService : ITextExtractionService
    {
        public Task<TextExtractionResult> ExtractAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TextExtractionResult(string.Empty, "text/plain", true));
        }
    }

    private sealed class NoOpChunkingService : IChunkingService
    {
        public Task<IReadOnlyList<TextChunk>> ChunkAsync(string text, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<TextChunk>>(Array.Empty<TextChunk>());
        }
    }

    private sealed class NoOpEmbeddingService : IEmbeddingService
    {
        public Task<IReadOnlyList<EmbeddedChunk>> GenerateAsync(IReadOnlyList<TextChunk> chunks, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<EmbeddedChunk>>(Array.Empty<EmbeddedChunk>());
        }
    }

    private sealed class NoOpDocumentIndexService : IDocumentIndexService
    {
        public Task IndexAsync(Document document, TextExtractionResult extraction, IReadOnlyList<EmbeddedChunk> embeddedChunks, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task MarkDeletedAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
