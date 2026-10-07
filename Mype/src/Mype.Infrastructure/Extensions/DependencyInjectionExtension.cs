using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Common.Interfaces;
using Mype.Application.Users.Interfaces;
using Mype.Infrastructure.Auth;
using Mype.Infrastructure.Persistence;
using Mype.Infrastructure.Users.Repositories;
using System.Diagnostics.CodeAnalysis;

namespace Mype.Infrastructure.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjectionExtension
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddPersistence(configuration);
            services.AddAuth();

            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IUnitOfWork>(provider =>
                provider.GetRequiredService<MypeDbContext>()
            );

            return services;
        }
    }
}
