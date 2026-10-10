using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Common.Exceptions;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Infrastructure.FinancialMovements.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.FinancialMovements.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class ExpenseItemConcurrencyIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public ExpenseItemConcurrencyIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Reject_Stale_MovementVersion()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            var ids = await SeedMovementAsync(factory);

            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);
            var movement = await repository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var item = movement.Items.Single();
            var staleMovementVersion = movement.Version + 100u;

            repository.SetOriginalVersions(movement, staleMovementVersion, item, item.Version);
            movement.UpdateExpenseItem(
                item.Id,
                ids.CategoryId,
                "Movimiento obsoleto",
                2m,
                5m,
                ids.UserId,
                RepositoryTestData.UtcNow.AddMinutes(1)
            );

            var action = () => context.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
            await AssertDatabaseUnchangedAsync(factory, ids.MovementId, "Inicial", 5m);
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Reject_Stale_ItemVersion()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            var ids = await SeedMovementAsync(factory);

            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);
            var movement = await repository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var item = movement.Items.Single();
            var staleItemVersion = item.Version + 100u;

            repository.SetOriginalVersions(movement, movement.Version, item, staleItemVersion);
            movement.UpdateExpenseItem(
                item.Id,
                ids.CategoryId,
                "Ítem obsoleto",
                3m,
                5m,
                ids.UserId,
                RepositoryTestData.UtcNow.AddMinutes(1)
            );

            var action = () => context.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
            await AssertDatabaseUnchangedAsync(factory, ids.MovementId, "Inicial", 5m);
        }

        private static async Task AssertDatabaseUnchangedAsync(
            MypeWebApplicationFactory factory,
            Guid movementId,
            string expectedDescription,
            decimal expectedTotal
        )
        {
            await using var verificationScope = factory.Services.CreateAsyncScope();
            var verificationContext =
                verificationScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var persisted = await verificationContext
                .FinancialMovements.AsNoTracking()
                .Include(movement => movement.Items)
                .SingleAsync(movement => movement.Id == movementId);

            persisted.TotalAmount.Should().Be(expectedTotal);
            persisted.Items.Single().Description.Should().Be(expectedDescription);
        }

        private static async Task<MovementIds> SeedMovementAsync(MypeWebApplicationFactory factory)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var category = Category.CreateDefault(
                seed.Business.Id,
                CategoryType.Expense,
                "Gastos",
                $"GASTOS {seed.Suffix}".ToUpperInvariant(),
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            context.Categories.Add(category);
            var movement = FinancialMovement.CreateDraft(
                seed.Business.Id,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            movement.AddExpenseItem(
                category.Id,
                "Inicial",
                1m,
                5m,
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            context.FinancialMovements.Add(movement);
            await context.SaveChangesAsync(CancellationToken.None);

            return new MovementIds(movement.Id, seed.Business.Id, category.Id, seed.User.Id);
        }

        private sealed record MovementIds(
            Guid MovementId,
            Guid BusinessId,
            Guid CategoryId,
            Guid UserId
        );
    }
}
