using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DocIndexService.Infrastructure.Configuration;

public static class SharedHostConfigurationExtensions
{
    public static IConfigurationBuilder AddDocIndexSharedConfiguration(
        this IConfigurationBuilder configurationBuilder,
        IHostEnvironment environment)
    {
        return configurationBuilder
            .AddJsonFile("appsettings.shared.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.shared.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "DOCINDEX_");
    }
}
