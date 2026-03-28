using DocIndexService.Application.Abstractions.Persistence;
using DocIndexService.Application.Abstractions.Persistence.Repositories;
using DocIndexService.Application.Abstractions.Api.Documents;
using DocIndexService.Application.Abstractions.Api.Jobs;
using DocIndexService.Application.Abstractions.Api.Sources;
using DocIndexService.Application.Abstractions.Dashboard;
using DocIndexService.Application.Abstractions.External;
using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Application.Abstractions.Search;
using DocIndexService.Application.Abstractions.SourceManagement;
using DocIndexService.Core.Interfaces;
using DocIndexService.Core.Options;
using DocIndexService.Infrastructure.Persistence;
using DocIndexService.Infrastructure.Persistence.Repositories;
using DocIndexService.Infrastructure.Clients;
using DocIndexService.Infrastructure.Services.Api.Documents;
using DocIndexService.Infrastructure.Services.Api.Jobs;
using DocIndexService.Infrastructure.Services.Api.Sources;
using DocIndexService.Infrastructure.Services.Dashboard;
using DocIndexService.Infrastructure.Services.Ingestion;
using DocIndexService.Infrastructure.Services.Search;
using DocIndexService.Infrastructure.Services.SourceManagement;
using DocIndexService.Infrastructure.Services.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pgvector.EntityFrameworkCore;

namespace DocIndexService.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection(PostgresOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<TikaOptions>()
            .Bind(configuration.GetSection(TikaOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<ScanOptions>()
            .Bind(configuration.GetSection(ScanOptions.SectionName))
            .ValidateOnStart();

        services.AddDbContext<DocIndexDbContext>((serviceProvider, optionsBuilder) =>
        {
            var postgresOptions = serviceProvider.GetRequiredService<IOptions<PostgresOptions>>().Value;

            optionsBuilder.UseNpgsql(
                postgresOptions.ConnectionString,
                npgsql =>
                {
                    npgsql.UseVector();
                    if (!string.IsNullOrWhiteSpace(postgresOptions.MigrationsAssembly))
                    {
                        npgsql.MigrationsAssembly(postgresOptions.MigrationsAssembly);
                    }
                });
        });

        services.AddScoped<IDocIndexDbContext>(sp => sp.GetRequiredService<DocIndexDbContext>());

        services.AddScoped<IDocumentSourceRepository, DocumentSourceRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IIngestionJobRepository, IngestionJobRepository>();

        services.AddSingleton<IClock, SystemClock>();

        services.AddHttpClient<ITikaClient, TikaHttpClient>();
        services.AddHttpClient<IOllamaClient, OllamaHttpClient>();

        services.AddScoped<IFileScanner, FileScannerService>();
        services.AddScoped<IFileFingerprintService, FileFingerprintService>();
        services.AddScoped<ITextExtractionService, TextExtractionService>();
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IDocumentIndexService, DocumentIndexService>();
        services.AddScoped<IIngestionCoordinator, IngestionCoordinator>();

        services.AddScoped<IDocumentSourceService, DocumentSourceService>();
        services.AddScoped<IDashboardStatsService, DashboardStatsService>();

        services.AddScoped<ISourceApiService, SourceApiService>();
        services.AddScoped<IDocumentApiService, DocumentApiService>();
        services.AddScoped<IJobApiService, JobApiService>();

        services.AddScoped<ISearchService, PlaceholderSearchService>();

        return services;
    }
}
