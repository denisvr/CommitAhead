using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommitAhead.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
