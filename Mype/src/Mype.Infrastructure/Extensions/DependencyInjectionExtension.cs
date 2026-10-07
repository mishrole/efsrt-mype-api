using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mype.Infrastructure.Auth;
using Mype.Infrastructure.Persistence;
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

            // Example: services.AddTransient<IMyRepository, MyRepository>();
            // Example: services.AddTransient<IMyService, MyService>();
            // Example: services.AddSingleton<IMyProvider, MyProvider>();
            // Example: services.AddScoped<IMyQuery, MyQuery>();

            return services;
        }
    }
}
