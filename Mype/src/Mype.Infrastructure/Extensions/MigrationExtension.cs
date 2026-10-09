using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Mype.Infrastructure.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class MigrationExtension
    {
        public static async Task ApplyMigrationsAsync(
            this IServiceProvider services
        )
        {
            await using var scope = services.CreateAsyncScope();

            var logger = scope.ServiceProvider.GetRequiredService<ILogger<MypeDbContext>>();

            try
            {
                var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();

                await context.Database.MigrateAsync();

                logger.LogInformation(SuccessMessages.DatabaseMigrationSucceeded);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ErrorMessages.DatabaseMigrationFailed);

                throw;
            }
        }
    }
}
