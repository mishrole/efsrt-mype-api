using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Domain.Currencies;
using Mype.Infrastructure.Currencies.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.Currencies.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class CurrencyRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public CurrencyRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task GetActiveByCodeAsync_Should_Return_Detached_PEN()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new CurrencyRepository(context);

            var result = await repository.GetActiveByCodeAsync("PEN", CancellationToken.None);

            result.Should().NotBeNull();
            result.Code.Should().Be("PEN");
            result.IsActive.Should().BeTrue();
            context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetActiveByCodeAsync_Should_Return_Null_For_Unknown_Code()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var repository = new CurrencyRepository(
                scope.ServiceProvider.GetRequiredService<MypeDbContext>()
            );

            (await repository.GetActiveByCodeAsync("ZZZ", CancellationToken.None))
                .Should()
                .BeNull();
        }
    }
}
