using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Domain.Businesses;
using Mype.Infrastructure.Businesses.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.Businesses.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class BusinessRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public BusinessRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task AddAsync_And_ExistsByRucAsync_Should_Persist_And_Query_Business()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var currency = await context
                .Currencies.AsNoTracking()
                .SingleAsync(item => item.Code == "PEN");
            var repository = new BusinessRepository(context);
            var ruc = $"20{System.Random.Shared.NextInt64(100000000, 999999999)}";
            var business = Business.Create(
                "Negocio con RUC",
                "Razón social",
                ruc,
                currency.Id,
                seed.User.Id,
                RepositoryTestData.UtcNow
            );

            await repository.AddAsync(business, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            (await repository.ExistsByRucAsync(ruc, CancellationToken.None)).Should().BeTrue();
            (await repository.ExistsByRucAsync("20999999999", CancellationToken.None))
                .Should()
                .BeFalse();
            context.ChangeTracker.Entries<Business>().Should().BeEmpty();
        }
    }
}
