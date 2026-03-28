using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace DocIndexService.Infrastructure.Persistence.DesignTime;

public sealed class DocIndexDbContextFactory : IDesignTimeDbContextFactory<DocIndexDbContext>
{
    public DocIndexDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DocIndexDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("DOCINDEX_POSTGRES_CONNECTIONSTRING")
            ?? "Host=localhost;Port=5432;Database=docindexservice;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.UseVector());

        return new DocIndexDbContext(optionsBuilder.Options);
    }
}
