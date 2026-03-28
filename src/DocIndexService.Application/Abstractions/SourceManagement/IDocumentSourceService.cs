namespace DocIndexService.Application.Abstractions.SourceManagement;

public interface IDocumentSourceService
{
    Task<IReadOnlyList<DocumentSourceDto>> ListAsync(CancellationToken cancellationToken);
    Task<DocumentSourceDto?> GetByIdAsync(Guid sourceId, CancellationToken cancellationToken);
    Task<SourceValidationResult> ValidateAsync(CreateDocumentSourceRequest request, CancellationToken cancellationToken);
    Task<SourceValidationResult> ValidateAsync(UpdateDocumentSourceRequest request, CancellationToken cancellationToken);
    Task<SourceOperationResult> CreateAsync(CreateDocumentSourceRequest request, CancellationToken cancellationToken);
    Task<SourceOperationResult> UpdateAsync(Guid sourceId, UpdateDocumentSourceRequest request, CancellationToken cancellationToken);
    Task<SourceOperationResult> DeleteAsync(Guid sourceId, CancellationToken cancellationToken);
    Task<SourceOperationResult> SetEnabledAsync(Guid sourceId, bool isEnabled, CancellationToken cancellationToken);
    Task<SourceOperationResult> TriggerManualScanAsync(ManualScanRequest request, CancellationToken cancellationToken);
}
