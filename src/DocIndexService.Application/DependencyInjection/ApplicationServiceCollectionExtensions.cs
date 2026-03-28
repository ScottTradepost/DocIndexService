using Microsoft.Extensions.DependencyInjection;

namespace DocIndexService.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // TODO: Register real application services, validators, and use-case handlers.
        return services;
    }
}
