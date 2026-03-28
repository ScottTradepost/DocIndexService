using System.Text.Json;
using System.Linq.Expressions;
using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Application.Abstractions.SourceManagement;
using DocIndexService.Core.Entities;
using DocIndexService.Core.Interfaces;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocIndexService.Infrastructure.Services.SourceManagement;

public sealed class DocumentSourceService : IDocumentSourceService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly DocIndexDbContext _dbContext;
    private readonly IIngestionCoordinator _ingestionCoordinator;
    private readonly IClock _clock;
    private readonly ILogger<DocumentSourceService> _logger;

    public DocumentSourceService(
        DocIndexDbContext dbContext,
        IIngestionCoordinator ingestionCoordinator,
        IClock clock,
        ILogger<DocumentSourceService> logger)
    {
        _dbContext = dbContext;
        _ingestionCoordinator = ingestionCoordinator;
        _clock = clock;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DocumentSourceDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.DocumentSourcesSet
            .OrderBy(x => x.Name)
            .Select(Map)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentSourceDto?> GetByIdAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        return await _dbContext.DocumentSourcesSet
            .Where(x => x.Id == sourceId)
            .Select(Map)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<SourceValidationResult> ValidateAsync(CreateDocumentSourceRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(ValidateInternal(request.Name, request.RootPath, request.ScanIntervalMinutes, request.IncludePatterns));
    }

    public Task<SourceValidationResult> ValidateAsync(UpdateDocumentSourceRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(ValidateInternal(request.Name, request.RootPath, request.ScanIntervalMinutes, request.IncludePatterns));
    }

    public async Task<SourceOperationResult> CreateAsync(CreateDocumentSourceRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateInternal(request.Name, request.RootPath, request.ScanIntervalMinutes, request.IncludePatterns);
        if (!validation.IsValid)
        {
            return new SourceOperationResult(false, string.Join(" ", validation.Errors));
        }

        var duplicate = await _dbContext.DocumentSourcesSet
            .AnyAsync(x => x.Name == request.Name.Trim(), cancellationToken);
        if (duplicate)
        {
            return new SourceOperationResult(false, "Source name must be unique.");
        }

        var now = _clock.UtcNow;
        var source = new DocumentSource
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            RootPath = request.RootPath.Trim(),
            IsRecursive = request.IsRecursive,
            IncludePatterns = NormalizePatterns(request.IncludePatterns, "*"),
            ExcludePatterns = NormalizePatterns(request.ExcludePatterns, string.Empty),
            ScanIntervalMinutes = request.ScanIntervalMinutes,
            IsEnabled = request.IsEnabled,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        await _dbContext.DocumentSourcesSet.AddAsync(source, cancellationToken);
        AddAuditLog("SourceCreated", source.Id, new { source.Name, source.RootPath });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created document source {SourceId} at path {RootPath}", source.Id, source.RootPath);

        return new SourceOperationResult(true, "Source created.");
    }

    public async Task<SourceOperationResult> UpdateAsync(Guid sourceId, UpdateDocumentSourceRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateInternal(request.Name, request.RootPath, request.ScanIntervalMinutes, request.IncludePatterns);
        if (!validation.IsValid)
        {
            return new SourceOperationResult(false, string.Join(" ", validation.Errors));
        }

        var source = await _dbContext.DocumentSourcesSet.FirstOrDefaultAsync(x => x.Id == sourceId, cancellationToken);
        if (source is null)
        {
            return new SourceOperationResult(false, "Source not found.");
        }

        var duplicate = await _dbContext.DocumentSourcesSet
            .AnyAsync(x => x.Id != sourceId && x.Name == request.Name.Trim(), cancellationToken);
        if (duplicate)
        {
            return new SourceOperationResult(false, "Source name must be unique.");
        }

        source.Name = request.Name.Trim();
        source.RootPath = request.RootPath.Trim();
        source.IsRecursive = request.IsRecursive;
        source.IncludePatterns = NormalizePatterns(request.IncludePatterns, "*");
        source.ExcludePatterns = NormalizePatterns(request.ExcludePatterns, string.Empty);
        source.ScanIntervalMinutes = request.ScanIntervalMinutes;
        source.IsEnabled = request.IsEnabled;
        source.UpdatedUtc = _clock.UtcNow;

        AddAuditLog("SourceUpdated", source.Id, new { source.Name, source.RootPath });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SourceOperationResult(true, "Source updated.");
    }

    public async Task<SourceOperationResult> DeleteAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        var source = await _dbContext.DocumentSourcesSet.FirstOrDefaultAsync(x => x.Id == sourceId, cancellationToken);
        if (source is null)
        {
            return new SourceOperationResult(false, "Source not found.");
        }

        var hasDocuments = await _dbContext.DocumentsSet.AnyAsync(x => x.SourceId == sourceId, cancellationToken);
        var hasJobs = await _dbContext.IngestionJobsSet.AnyAsync(x => x.SourceId == sourceId, cancellationToken);
        if (hasDocuments || hasJobs)
        {
            return new SourceOperationResult(false, "Source cannot be deleted after documents or jobs exist; disable it instead.");
        }

        _dbContext.DocumentSourcesSet.Remove(source);
        AddAuditLog("SourceDeleted", source.Id, new { source.Name, source.RootPath });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SourceOperationResult(true, "Source deleted.");
    }

    public async Task<SourceOperationResult> SetEnabledAsync(Guid sourceId, bool isEnabled, CancellationToken cancellationToken)
    {
        var source = await _dbContext.DocumentSourcesSet.FirstOrDefaultAsync(x => x.Id == sourceId, cancellationToken);
        if (source is null)
        {
            return new SourceOperationResult(false, "Source not found.");
        }

        source.IsEnabled = isEnabled;
        source.UpdatedUtc = _clock.UtcNow;

        AddAuditLog(isEnabled ? "SourceEnabled" : "SourceDisabled", source.Id, new { source.Name, source.RootPath });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SourceOperationResult(true, isEnabled ? "Source enabled." : "Source disabled.");
    }

    public async Task<SourceOperationResult> TriggerManualScanAsync(ManualScanRequest request, CancellationToken cancellationToken)
    {
        var sourceExists = await _dbContext.DocumentSourcesSet.AnyAsync(x => x.Id == request.SourceId, cancellationToken);
        if (!sourceExists)
        {
            return new SourceOperationResult(false, "Source not found.");
        }

        try
        {
            await _ingestionCoordinator.TriggerScanAsync(request.SourceId, request.FullReconciliation, cancellationToken);
            AddAuditLog(
                request.FullReconciliation ? "SourceManualFullScan" : "SourceManualIncrementalScan",
                request.SourceId,
                new { request.FullReconciliation });
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new SourceOperationResult(true, "Manual scan completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual scan failed for source {SourceId}", request.SourceId);
            return new SourceOperationResult(false, "Manual scan failed. Check logs for details.");
        }
    }

    private static SourceValidationResult ValidateInternal(string name, string rootPath, int scanIntervalMinutes, string includePatterns)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            errors.Add("Root path is required.");
        }
        else if (!Path.IsPathFullyQualified(rootPath))
        {
            errors.Add("Root path must be absolute.");
        }

        if (scanIntervalMinutes < 1 || scanIntervalMinutes > 1440)
        {
            errors.Add("Scan interval must be between 1 and 1440 minutes.");
        }

        if (string.IsNullOrWhiteSpace(includePatterns))
        {
            errors.Add("At least one include pattern is required.");
        }

        var pathExists = Directory.Exists(rootPath);
        var pathIsAccessible = false;
        if (pathExists)
        {
            try
            {
                _ = Directory.EnumerateFileSystemEntries(rootPath).Take(1).ToList();
                pathIsAccessible = true;
            }
            catch
            {
                pathIsAccessible = false;
                errors.Add("Root path is not accessible.");
            }
        }
        else
        {
            errors.Add("Root path does not exist.");
        }

        return new SourceValidationResult(errors.Count == 0, errors, pathExists, pathIsAccessible);
    }

    private static string NormalizePatterns(string patterns, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(patterns))
        {
            return defaultValue;
        }

        var normalized = patterns
            .Split([';', ',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return normalized.Length == 0 ? defaultValue : string.Join(';', normalized);
    }

    private void AddAuditLog(string actionType, Guid sourceId, object details)
    {
        var now = _clock.UtcNow;
        _dbContext.AuditLogsSet.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = actionType,
            EntityType = "DocumentSource",
            EntityId = sourceId.ToString(),
            DetailsJson = JsonSerializer.Serialize(details, SerializerOptions),
            CreatedUtc = now,
            UpdatedUtc = now
        });
    }

    private static readonly Expression<Func<DocumentSource, DocumentSourceDto>> Map = source => new DocumentSourceDto(
        source.Id,
        source.Name,
        source.RootPath,
        source.IsEnabled,
        source.IsRecursive,
        source.IncludePatterns,
        source.ExcludePatterns,
        source.ScanIntervalMinutes,
        source.LastScanUtc,
        source.LastSuccessfulScanUtc,
        source.CreatedUtc,
        source.UpdatedUtc);
}
