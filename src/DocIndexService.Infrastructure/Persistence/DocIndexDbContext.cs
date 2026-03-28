using DocIndexService.Application.Abstractions.Persistence;
using DocIndexService.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Infrastructure.Persistence;

public sealed class DocIndexDbContext : DbContext, IDocIndexDbContext
{
    public DocIndexDbContext(DbContextOptions<DocIndexDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> UsersSet => Set<User>();
    public DbSet<Role> RolesSet => Set<Role>();
    public DbSet<UserRole> UserRolesSet => Set<UserRole>();
    public DbSet<ApiClient> ApiClientsSet => Set<ApiClient>();
    public DbSet<DocumentSource> DocumentSourcesSet => Set<DocumentSource>();
    public DbSet<Document> DocumentsSet => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersionsSet => Set<DocumentVersion>();
    public DbSet<DocumentChunk> DocumentChunksSet => Set<DocumentChunk>();
    public DbSet<IngestionJob> IngestionJobsSet => Set<IngestionJob>();
    public DbSet<IngestionJobEvent> IngestionJobEventsSet => Set<IngestionJobEvent>();
    public DbSet<AuditLog> AuditLogsSet => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettingsSet => Set<SystemSetting>();

    public IQueryable<User> Users => UsersSet.AsQueryable();
    public IQueryable<Role> Roles => RolesSet.AsQueryable();
    public IQueryable<UserRole> UserRoles => UserRolesSet.AsQueryable();
    public IQueryable<ApiClient> ApiClients => ApiClientsSet.AsQueryable();
    public IQueryable<DocumentSource> DocumentSources => DocumentSourcesSet.AsQueryable();
    public IQueryable<Document> Documents => DocumentsSet.AsQueryable();
    public IQueryable<DocumentVersion> DocumentVersions => DocumentVersionsSet.AsQueryable();
    public IQueryable<DocumentChunk> DocumentChunks => DocumentChunksSet.AsQueryable();
    public IQueryable<IngestionJob> IngestionJobs => IngestionJobsSet.AsQueryable();
    public IQueryable<IngestionJobEvent> IngestionJobEvents => IngestionJobEventsSet.AsQueryable();
    public IQueryable<AuditLog> AuditLogs => AuditLogsSet.AsQueryable();
    public IQueryable<SystemSetting> SystemSettings => SystemSettingsSet.AsQueryable();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocIndexDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
