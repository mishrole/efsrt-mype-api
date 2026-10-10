using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mype.Domain.Businesses;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence;

namespace Mype.Tests.Infrastructure.Common
{
    internal static class RepositoryTestData
    {
        internal static readonly DateTimeOffset UtcNow = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        internal static async Task<BusinessSeed> SeedBusinessAsync(MypeDbContext context)
        {
            var suffix = Guid.NewGuid().ToString("N");
            var user = User.Create(
                $"repository-{suffix}@example.com",
                $"REPOSITORY-{suffix}@EXAMPLE.COM",
                "hash",
                "Repository Test User",
                UtcNow
            );
            var currency = await context
                .Currencies.AsNoTracking()
                .SingleAsync(current => current.Code == "PEN");
            var business = Business.Create(
                $"Business {suffix}",
                null,
                null,
                currency.Id,
                user.Id,
                UtcNow
            );

            context.Users.Add(user);
            context.Businesses.Add(business);
            await context.SaveChangesAsync(CancellationToken.None);

            return new(user, business, suffix);
        }
    }

    internal sealed record BusinessSeed(User User, Business Business, string Suffix);
}
