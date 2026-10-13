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

namespace Mype.Tests.Infrastructure.FinancialDashboard.Repositories
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

        [Fact]
        public async Task GetAsync_Should_Exclude_Confirmed_Movements_Before_Opening_Date()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var openingDate = new DateOnly(2026, 10, 1);
            var category = Mype.Domain.Categories.Category.CreateDefault(
                seed.Business.Id,
                Mype.Domain.Categories.CategoryType.Expense,
                "Prueba dashboard",
                $"PRUEBA DASHBOARD {seed.Suffix}",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            var before = FinancialMovement.CreateDraft(
                seed.Business.Id,
                FinancialMovementType.Expense,
                openingDate.AddDays(-1),
                "Anterior",
                "PEN",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            before.AddExpenseItem(
                category.Id,
                "Anterior",
                1m,
                100m,
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            before.Confirm(seed.User.Id, RepositoryTestData.UtcNow);
            var onOpening = FinancialMovement.CreateDraft(
                seed.Business.Id,
                FinancialMovementType.Expense,
                openingDate,
                "En apertura",
                "PEN",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            onOpening.AddExpenseItem(
                category.Id,
                "En apertura",
                1m,
                200m,
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            onOpening.Confirm(seed.User.Id, RepositoryTestData.UtcNow);
            context.Categories.Add(category);
            context.FinancialMovements.AddRange(
                FinancialMovement.CreateOpeningBalance(
                    seed.Business.Id,
                    openingDate,
                    1000m,
                    "PEN",
                    seed.User.Id,
                    RepositoryTestData.UtcNow
                ),
                before,
                onOpening
            );
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var result = await new FinancialDashboardRepository(context).GetAsync(
                seed.Business.Id,
                openingDate,
                CancellationToken.None
            );

            result
                .Movements.Should()
                .ContainSingle(movement =>
                    movement.MovementDate == openingDate && movement.TotalAmount == 200m
                );
            result
                .Categories.Should()
                .ContainSingle(item => item.MovementDate == openingDate && item.Amount == 200m);
        }
    }
}
