using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mype.Domain.FinancialMovements;
using Mype.Infrastructure.FinancialDashboard.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.FinancialDashboard
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class FinancialDashboardRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public FinancialDashboardRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Theory]
        [InlineData(2500.00)]
        [InlineData(0.00)]
        [InlineData(-500.00)]
        public async Task GetAsync_Should_Return_Current_Signed_Opening_Without_Tracking(
            decimal amount
        )
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            context.FinancialMovements.Add(
                FinancialMovement.CreateOpeningBalance(
                    seed.Business.Id,
                    new DateOnly(2026, 1, 1),
                    amount,
                    "PEN",
                    seed.User.Id,
                    RepositoryTestData.UtcNow
                )
            );
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var result = await new FinancialDashboardRepository(context).GetAsync(
                seed.Business.Id,
                new DateOnly(2026, 12, 31),
                CancellationToken.None
            );

            result.Should().NotBeNull();
            result.OpeningBalanceAmount.Should().Be(amount);
            result.Movements.Should().BeEmpty();
            result.Categories.Should().BeEmpty();
            context.ChangeTracker.Entries().Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsync_Should_Return_Null_When_Business_Has_No_Current_Opening()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);

            var result = await new FinancialDashboardRepository(context).GetAsync(
                seed.Business.Id,
                new DateOnly(2026, 12, 31),
                CancellationToken.None
            );

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAsync_Should_Isolate_Opening_By_Business()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var first = await RepositoryTestData.SeedBusinessAsync(context);
            var second = await RepositoryTestData.SeedBusinessAsync(context);
            context.FinancialMovements.Add(
                FinancialMovement.CreateOpeningBalance(
                    first.Business.Id,
                    new DateOnly(2026, 1, 1),
                    100m,
                    "PEN",
                    first.User.Id,
                    RepositoryTestData.UtcNow
                )
            );
            context.FinancialMovements.Add(
                FinancialMovement.CreateOpeningBalance(
                    second.Business.Id,
                    new DateOnly(2026, 1, 1),
                    900m,
                    "PEN",
                    second.User.Id,
                    RepositoryTestData.UtcNow
                )
            );
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var result = await new FinancialDashboardRepository(context).GetAsync(
                first.Business.Id,
                new DateOnly(2026, 12, 31),
                CancellationToken.None
            );

            result.OpeningBalanceAmount.Should().Be(100m);
        }
    }
}
