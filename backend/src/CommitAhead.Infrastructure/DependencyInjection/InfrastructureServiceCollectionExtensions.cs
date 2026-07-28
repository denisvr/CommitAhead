using CommitAhead.Application.Identity;
using CommitAhead.Infrastructure.Identity;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommitAhead.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CommitAheadDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CommitAheadDb")));

        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
