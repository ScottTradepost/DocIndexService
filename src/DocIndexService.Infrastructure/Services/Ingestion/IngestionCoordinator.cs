using System.Linq.Expressions;
using System.Text.Json;
using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Core.Entities;
using DocIndexService.Core.Enums;
using DocIndexService.Core.Interfaces;
using DocIndexService.Core.Options;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocIndexService.Infrastructure.Services.Ingestion;

public sealed class IngestionCoordinator : IIngestionCoordinator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly DocIndexDbContext _dbContext;
    private readonly IFileScanner _fileScanner;
    private readonly IFileFingerprintService _fileFingerprintService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentIndexService _documentIndexService;
    private readonly IClock _clock;
    private readonly ScanOptions _scanOptions;
    private readonly ILogger<IngestionCoordinator> _logger;

    public IngestionCoordinator(
        DocIndexDbContext dbContext,
        IFileScanner fileScanner,
        IFileFingerprintService fileFingerprintService,
        ITextExtractionService textExtractionService,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IDocumentIndexService documentIndexService,
        IClock clock,
        IOptions<ScanOptions> scanOptions,
        ILogger<IngestionCoordinator> logger)
    {
        _dbContext = dbContext;
        _fileScanner = fileScanner;
        _fileFingerprintService = fileFingerprintService;
        _textExtractionService = textExtractionService;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _documentIndexService = documentIndexService;
        _clock = clock;
        _scanOptions = scanOptions.Value;
        _logger = logger;
    }

    public async Task RunScheduledScanCycleAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var enabledSources = await _dbContext.DocumentSourcesSet
            .Where(x => x.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var source in enabledSources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullReconciliationDue = now.Hour >= _scanOptions.FullReconciliationHourUtc
                && source.LastSuccessfulScanUtc?.Date != now.Date;

            if (fullReconciliationDue)
            {
                await RunSourceScanAsync(source, fullReconciliation: true, cancellationToken);
                continue;
            }

            var incrementalDue = source.LastScanUtc is null
                || (now - source.LastScanUtc.Value) >= TimeSpan.FromMinutes(Math.Max(1, source.ScanIntervalMinutes));

            if (incrementalDue)
            {
                await RunSourceScanAsync(source, fullReconciliation: false, cancellationToken);
            }
        }
    }

    public async Task TriggerScanAsync(Guid sourceId, bool fullReconciliation, CancellationToken cancellationToken)
    {
        var source = await _dbContext.DocumentSourcesSet.FirstOrDefaultAsync(x => x.Id == sourceId, cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException($"Source {sourceId} was not found.");
        }

        await RunSourceScanAsync(source, fullReconciliation, cancellationToken);
    }

    public async Task ProcessPendingJobsAsync(int take, CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.IngestionJobsSet
            .Where(x => x.Status == IngestionJobStatus.Pending)
            .OrderBy(x => x.CreatedUtc)
            .Take(Math.Max(1, take))
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await StartJobAsync(job, cancellationToken);
                await ExecuteJobAsync(job, cancellationToken);
                await CompleteJobAsync(job, succeeded: true, errorMessage: null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ingestion job {JobId} failed", job.Id);
                await CompleteJobAsync(job, succeeded: false, errorMessage: ex.Message, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RunSourceScanAsync(DocumentSource source, bool fullReconciliation, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var scanJob = new IngestionJob
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            JobType = fullReconciliation ? IngestionJobType.ScanFullReconciliation : IngestionJobType.ScanIncremental,
            Status = IngestionJobStatus.Running,
            StartedUtc = now,
            AttemptCount = 1,
            PayloadJson = JsonSerializer.Serialize(new { source.RootPath, fullReconciliation }, SerializerOptions),
            CreatedUtc = now,
            UpdatedUtc = now
        };

        await _dbContext.IngestionJobsSet.AddAsync(scanJob, cancellationToken);
        await AddJobEventAsync(scanJob.Id, "ScanStarted", $"Scan started for source '{source.Name}'", new { fullReconciliation }, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            _logger.LogInformation(
                "Starting {Mode} scan for source {SourceId} ({RootPath})",
                fullReconciliation ? "full" : "incremental",
                source.Id,
                source.RootPath);

            var snapshot = await _fileScanner.ScanAsync(source, fullReconciliation, cancellationToken);
            var existingDocuments = await _dbContext.DocumentsSet
                .Where(x => x.SourceId == source.Id)
                .ToListAsync(cancellationToken);

            var existingByPath = existingDocuments.ToDictionary(x => x.RelativePath, StringComparer.OrdinalIgnoreCase);
            var scannedByPath = snapshot.Files.ToDictionary(x => x.RelativePath, StringComparer.OrdinalIgnoreCase);

            var newCount = 0;
            var updatedCount = 0;
            var deletedCount = 0;

            foreach (var scanned in snapshot.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!existingByPath.TryGetValue(scanned.RelativePath, out var existing))
                {
                    var fingerprint = await _fileFingerprintService.ComputeAsync(scanned.FullPath, cancellationToken);
                    var document = new Document
                    {
                        Id = Guid.NewGuid(),
                        SourceId = source.Id,
                        RelativePath = scanned.RelativePath,
                        FullPath = scanned.FullPath,
                        FileName = Path.GetFileName(scanned.FullPath),
                        Extension = scanned.Extension,
                        Sha256 = fingerprint.Sha256,
                        FileSize = fingerprint.FileSize,
                        FileLastModifiedUtc = fingerprint.LastModifiedUtc,
                        Status = DocumentStatus.Pending,
                        IsDeleted = false,
                        Title = Path.GetFileNameWithoutExtension(scanned.FullPath),
                        MetadataJson = "{}",
                        CreatedUtc = now,
                        UpdatedUtc = now
                    };

                    await _dbContext.DocumentsSet.AddAsync(document, cancellationToken);
                    await EnqueueDocumentJobAsync(source.Id, document.Id, IngestionOperation.New, scanned.RelativePath, scanned.FullPath, fullReconciliation, cancellationToken);
                    newCount++;
                    continue;
                }

                var metadataChanged = existing.FileLastModifiedUtc != scanned.LastModifiedUtc || existing.FileSize != scanned.FileSize;
                if (!metadataChanged && !existing.IsDeleted)
                {
                    continue;
                }

                var fp = await _fileFingerprintService.ComputeAsync(scanned.FullPath, cancellationToken);
                var contentChanged = existing.Sha256 != fp.Sha256 || existing.IsDeleted;
                if (!contentChanged)
                {
                    existing.FullPath = scanned.FullPath;
                    existing.FileName = Path.GetFileName(scanned.FullPath);
                    existing.Extension = scanned.Extension;
                    existing.FileSize = fp.FileSize;
                    existing.FileLastModifiedUtc = fp.LastModifiedUtc;
                    existing.UpdatedUtc = now;
                    continue;
                }

                existing.FullPath = scanned.FullPath;
                existing.FileName = Path.GetFileName(scanned.FullPath);
                existing.Extension = scanned.Extension;
                existing.Sha256 = fp.Sha256;
                existing.FileSize = fp.FileSize;
                existing.FileLastModifiedUtc = fp.LastModifiedUtc;
                existing.IsDeleted = false;
                existing.Status = DocumentStatus.Pending;
                existing.UpdatedUtc = now;

                await EnqueueDocumentJobAsync(source.Id, existing.Id, IngestionOperation.Updated, scanned.RelativePath, scanned.FullPath, fullReconciliation, cancellationToken);
                updatedCount++;
            }

            foreach (var existing in existingDocuments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (existing.IsDeleted)
                {
                    continue;
                }

                if (scannedByPath.ContainsKey(existing.RelativePath))
                {
                    continue;
                }

                existing.IsDeleted = true;
                existing.Status = DocumentStatus.Pending;
                existing.UpdatedUtc = now;

                await EnqueueDocumentJobAsync(source.Id, existing.Id, IngestionOperation.Deleted, existing.RelativePath, existing.FullPath, fullReconciliation, cancellationToken);
                deletedCount++;
            }

            source.LastScanUtc = now;
            source.LastSuccessfulScanUtc = now;
            source.UpdatedUtc = now;

            scanJob.Status = IngestionJobStatus.Succeeded;
            scanJob.CompletedUtc = now;
            scanJob.UpdatedUtc = now;

            await AddJobEventAsync(
                scanJob.Id,
                "ScanCompleted",
                "Scan completed",
                new
                {
                    fullReconciliation,
                    totalFiles = snapshot.Files.Count,
                    newCount,
                    updatedCount,
                    deletedCount
                },
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Completed {Mode} scan for source {SourceId}: {New} new, {Updated} updated, {Deleted} deleted",
                fullReconciliation ? "full" : "incremental",
                source.Id,
                newCount,
                updatedCount,
                deletedCount);
        }
        catch (Exception ex)
        {
            source.LastScanUtc = now;
            source.UpdatedUtc = now;

            scanJob.Status = IngestionJobStatus.Failed;
            scanJob.ErrorMessage = ex.Message;
            scanJob.CompletedUtc = now;
            scanJob.UpdatedUtc = now;

            await AddJobEventAsync(scanJob.Id, "ScanFailed", "Scan failed", new { error = ex.Message }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Scan failed for source {SourceId}", source.Id);
            throw;
        }
    }

    private async Task ExecuteJobAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        var payload = DeserializePayload(job.PayloadJson);
        if (payload is null)
        {
            throw new InvalidOperationException($"Ingestion job payload is invalid for job {job.Id}.");
        }

        var document = await _dbContext.DocumentsSet.FirstOrDefaultAsync(x => x.Id == job.DocumentId, cancellationToken);
        if (document is null)
        {
            throw new InvalidOperationException($"Document {job.DocumentId} not found for job {job.Id}.");
        }

        if (payload.Operation == IngestionOperation.Deleted)
        {
            await _documentIndexService.MarkDeletedAsync(document, cancellationToken);
            await AddJobEventAsync(job.Id, "DocumentDeleted", "Marked document as deleted", payload, cancellationToken);
            return;
        }

        document.Status = DocumentStatus.Processing;
        document.UpdatedUtc = _clock.UtcNow;

        var extraction = await _textExtractionService.ExtractAsync(document, cancellationToken);
        await AddJobEventAsync(
            job.Id,
            "Extracted",
            "Extraction finished",
            new { extraction.IsPlaceholder, extraction.MimeType, payload.Operation },
            cancellationToken);

        var chunks = await _chunkingService.ChunkAsync(extraction.Text, cancellationToken);
        await AddJobEventAsync(job.Id, "Chunked", "Chunking finished", new { chunkCount = chunks.Count }, cancellationToken);

        var embeddedChunks = await _embeddingService.GenerateAsync(chunks, cancellationToken);
        await AddJobEventAsync(job.Id, "Embedded", "Embedding finished", new { chunkCount = embeddedChunks.Count }, cancellationToken);

        await _documentIndexService.IndexAsync(document, extraction, embeddedChunks, cancellationToken);
        await AddJobEventAsync(job.Id, "Indexed", "Index update finished", new { payload.Operation }, cancellationToken);
    }

    private async Task StartJobAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        job.Status = IngestionJobStatus.Running;
        job.AttemptCount += 1;
        job.StartedUtc = _clock.UtcNow;
        job.UpdatedUtc = job.StartedUtc;

        await AddJobEventAsync(job.Id, "JobStarted", "Job processing started", null, cancellationToken);
    }

    private async Task CompleteJobAsync(IngestionJob job, bool succeeded, string? errorMessage, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        job.Status = succeeded ? IngestionJobStatus.Succeeded : IngestionJobStatus.Failed;
        job.CompletedUtc = now;
        job.ErrorMessage = errorMessage;
        job.UpdatedUtc = now;

        if (!succeeded && job.DocumentId is { } docId)
        {
            var document = await _dbContext.DocumentsSet.FirstOrDefaultAsync(x => x.Id == docId, cancellationToken);
            if (document is not null)
            {
                document.Status = DocumentStatus.Failed;
                document.UpdatedUtc = now;
            }
        }

        await AddJobEventAsync(
            job.Id,
            succeeded ? "JobSucceeded" : "JobFailed",
            succeeded ? "Job processing finished" : "Job processing failed",
            new { errorMessage },
            cancellationToken);
    }

    private async Task EnqueueDocumentJobAsync(
        Guid sourceId,
        Guid documentId,
        IngestionOperation operation,
        string relativePath,
        string fullPath,
        bool fullReconciliation,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var payload = new DocumentJobPayload(relativePath, fullPath, operation, fullReconciliation);

        var job = new IngestionJob
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            DocumentId = documentId,
            JobType = operation == IngestionOperation.Deleted ? IngestionJobType.IndexDocument : IngestionJobType.ReprocessDocument,
            Status = IngestionJobStatus.Pending,
            StartedUtc = now,
            AttemptCount = 0,
            PayloadJson = JsonSerializer.Serialize(payload, SerializerOptions),
            CreatedUtc = now,
            UpdatedUtc = now
        };

        await _dbContext.IngestionJobsSet.AddAsync(job, cancellationToken);
        await AddJobEventAsync(
            job.Id,
            "ChangeDetected",
            $"Detected {operation} file change",
            payload,
            cancellationToken);
    }

    private async Task AddJobEventAsync(
        Guid ingestionJobId,
        string eventType,
        string? message,
        object? details,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        await _dbContext.IngestionJobEventsSet.AddAsync(new IngestionJobEvent
        {
            Id = Guid.NewGuid(),
            IngestionJobId = ingestionJobId,
            EventType = eventType,
            Message = message,
            DetailsJson = details is null ? "{}" : JsonSerializer.Serialize(details, SerializerOptions),
            CreatedUtc = now,
            UpdatedUtc = now
        }, cancellationToken);
    }

    private static DocumentJobPayload? DeserializePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<DocumentJobPayload>(payloadJson, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    private sealed record DocumentJobPayload(
        string RelativePath,
        string FullPath,
        IngestionOperation Operation,
        bool FullReconciliation);
}
