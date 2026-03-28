using DocIndexService.Core.Entities;

namespace DocIndexService.Application.Abstractions.Persistence;

public interface IDocIndexDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<Role> Roles { get; }
    IQueryable<UserRole> UserRoles { get; }
    IQueryable<ApiClient> ApiClients { get; }
    IQueryable<DocumentSource> DocumentSources { get; }
    IQueryable<Document> Documents { get; }
    IQueryable<DocumentVersion> DocumentVersions { get; }
    IQueryable<DocumentChunk> DocumentChunks { get; }
    IQueryable<IngestionJob> IngestionJobs { get; }
    IQueryable<IngestionJobEvent> IngestionJobEvents { get; }
    IQueryable<AuditLog> AuditLogs { get; }
    IQueryable<SystemSetting> SystemSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
