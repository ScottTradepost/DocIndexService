using DocIndexService.Application.Abstractions.Api.Sources;
using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Application.Abstractions.SourceManagement;
using DocIndexService.Contracts.Api.Sources;
using DocIndexService.Core.Entities;
using DocIndexService.Core.Enums;
using DocIndexService.Core.Interfaces;
using DocIndexService.Core.Options;
using DocIndexService.Infrastructure.Persistence;
using DocIndexService.Infrastructure.Services.Api.Sources;
using DocIndexService.Infrastructure.Services.Ingestion;
using DocIndexService.Infrastructure.Services.SourceManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocIndexService.Tests.Integration;

public sealed class ApiAndRetryIntegrationTests
{
    [Fact]
    public async Task RetryFailedJobAsync_ShouldQueuePendingRetryJob()
    {
        await using var db = CreateDbContext();
        var now = DateTime.UtcNow;
        var source = new DocumentSource
        {
            Id = Guid.NewGuid(),
            Name = "S1",
            RootPath = "C:/tmp",
            CreatedUtc = now,
            UpdatedUtc = now
        };
        var failedJob = new IngestionJob
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            JobType = IngestionJobType.ReprocessDocument,
            Status = IngestionJobStatus.Failed,
            StartedUtc = now.AddMinutes(-5),
            CompletedUtc = now.AddMinutes(-4),
            ErrorMessage = "boom",
            AttemptCount = 1,
            PayloadJson = "{}",
            CreatedUtc = now.AddMinutes(-5),
            UpdatedUtc = now.AddMinutes(-4)
        };

        await db.DocumentSourcesSet.AddAsync(source);
        await db.IngestionJobsSet.AddAsync(failedJob);
        await db.SaveChangesAsync();

        var coordinator = CreateCoordinator(db);
        var retryId = await coordinator.RetryFailedJobAsync(failedJob.Id, CancellationToken.None);

        Assert.NotNull(retryId);

        var queued = await db.IngestionJobsSet.FirstOrDefaultAsync(x => x.Id == retryId);
        Assert.NotNull(queued);
        Assert.Equal(IngestionJobStatus.Pending, queued!.Status);
        Assert.Equal(failedJob.SourceId, queued.SourceId);
        Assert.Equal(failedJob.JobType, queued.JobType);

        var events = await db.IngestionJobEventsSet.Where(x => x.IngestionJobId == queued.Id).ToListAsync();
        Assert.Contains(events, x => x.EventType == "RetriedFromFailedJob");
    }

    [Fact]
    public async Task EnqueueDocumentReprocessAsync_ShouldCreatePendingJob()
    {
        await using var db = CreateDbContext();
        var now = DateTime.UtcNow;
        var source = new DocumentSource
        {
            Id = Guid.NewGuid(),
            Name = "S1",
            RootPath = "C:/tmp",
            CreatedUtc = now,
            UpdatedUtc = now
        };
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            RelativePath = "a.txt",
            FullPath = "C:/tmp/a.txt",
            FileName = "a.txt",
            Extension = ".txt",
            Sha256 = "abc",
            FileSize = 10,
            FileLastModifiedUtc = now,
            Status = DocumentStatus.Indexed,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        await db.DocumentSourcesSet.AddAsync(source);
        await db.DocumentsSet.AddAsync(doc);
        await db.SaveChangesAsync();

        var coordinator = CreateCoordinator(db);
        var jobId = await coordinator.EnqueueDocumentReprocessAsync(doc.Id, CancellationToken.None);

        Assert.NotNull(jobId);

        var job = await db.IngestionJobsSet.FirstOrDefaultAsync(x => x.Id == jobId);
        Assert.NotNull(job);
        Assert.Equal(IngestionJobStatus.Pending, job!.Status);
        Assert.Equal(doc.Id, job.DocumentId);
        Assert.Equal(IngestionJobType.ReprocessDocument, job.JobType);
    }

    [Fact]
    public async Task SourceApiService_CreateAndList_ShouldReturnCreatedSource()
    {
        await using var db = CreateDbContext();
        var coordinator = CreateCoordinator(db);
        var sourceService = new DocumentSourceService(
            db,
            coordinator,
            new FixedClock(DateTime.UtcNow),
            NullLogger<DocumentSourceService>.Instance);

        ISourceApiService apiService = new SourceApiService(sourceService, new FixedClock(DateTime.UtcNow));

        var tempPath = Path.Combine(Path.GetTempPath(), "docindex-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        try
        {
            var create = await apiService.CreateAsync(
                new CreateSourceRequest(
                    "Source-1",
                    tempPath,
                    IsRecursive: true,
                    IncludePatterns: "*.txt",
                    ExcludePatterns: "",
                    ScanIntervalMinutes: 15,
                    IsEnabled: true),
                CancellationToken.None);

            Assert.True(create.Succeeded);

            var list = await apiService.ListAsync(CancellationToken.None);
            Assert.Equal(1, list.Count);
            Assert.Equal("Source-1", list.Items.First().Name);
        }
        finally
        {
            Directory.Delete(tempPath, recursive: true);
        }
    }

    private static DocIndexDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DocIndexDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new DocIndexDbContext(options);
    }

    private static IngestionCoordinator CreateCoordinator(DocIndexDbContext db)
    {
        return new IngestionCoordinator(
            db,
            new NoOpFileScanner(),
            new NoOpFingerprintService(),
            new NoOpExtractionService(),
            new NoOpChunkingService(),
            new NoOpEmbeddingService(),
            new NoOpDocumentIndexService(),
            new FixedClock(DateTime.UtcNow),
            Options.Create(new ScanOptions()),
            NullLogger<IngestionCoordinator>.Instance);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class NoOpFileScanner : IFileScanner
    {
        public Task<SourceScanSnapshot> ScanAsync(DocumentSource source, bool fullReconciliation, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SourceScanSnapshot(source.Id, source.RootPath, DateTime.UtcNow, Array.Empty<ScannedFileEntry>(), fullReconciliation));
        }
    }

    private sealed class NoOpFingerprintService : IFileFingerprintService
    {
        public Task<FileFingerprintResult> ComputeAsync(string fullPath, CancellationToken cancellationToken)
        {
            return Task.FromResult(new FileFingerprintResult("", DateTime.UtcNow, 0));
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

}
