using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mype.Shared.Constants;

namespace Mype.Infrastructure.Persistence
{
    [ExcludeFromCodeCoverage]
    public static class PersistenceSetup
    {
        public static IServiceCollection AddPersistence(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            var connectionString =
                configuration[Env.ConnectionStringKey]
                ?? throw new InvalidOperationException(
                    string.Format(ErrorMessages.VariableNotConfigured, Env.ConnectionStringKey)
                );

            services.AddDbContext<MypeDbContext>(opt =>
                opt.UseNpgsql(connectionString).UseSnakeCaseNamingConvention()
            );

            return services;
        }
    }
}
