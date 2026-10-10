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
    public sealed class ExpenseItemPersistenceIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public ExpenseItemPersistenceIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Persist_Expense_Item_With_Null_Product_Fields_And_Retire_Atomically()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var data = await SeedAsync(context);
            var repository = new FinancialMovementRepository(context);
            var movement = FinancialMovement.CreateDraft(
                data.BusinessId,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                data.UserId,
                RepositoryTestData.UtcNow
            );
            var item = movement.AddExpenseItem(
                data.CategoryId,
                "  Bolsas  ",
                2.1250m,
                15.50m,
                data.UserId,
                RepositoryTestData.UtcNow
            );
            await repository.AddAsync(movement, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var persisted = await context
                .FinancialMovements.Include(current => current.Items)
                .SingleAsync(current => current.Id == movement.Id);
            var persistedItem = persisted.Items.Single();
            persistedItem.ProductId.Should().BeNull();
            persistedItem.UnitCostSnapshot.Should().BeNull();
            persistedItem.Description.Should().Be("Bolsas");
            persistedItem.Quantity.Should().Be(2.1250m);
            persistedItem.UnitAmount.Should().Be(15.50m);
            persistedItem.SubtotalAmount.Should().Be(32.94m);
            persisted.TotalAmount.Should().Be(32.94m);

            persisted.RetireItem(
                persistedItem.Id,
                data.UserId,
                RepositoryTestData.UtcNow.AddMinutes(1)
            );
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var retired = await context
                .FinancialMovements.AsNoTracking()
                .Include(current => current.Items)
                .SingleAsync(current => current.Id == movement.Id);
            retired.Items.Single().IsActive.Should().BeFalse();
            retired.Items.Single().RetiredAt.Should().NotBeNull();
            retired.TotalAmount.Should().Be(0m);
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Translate_Stale_Expense_Item_Version()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            var ids = await SeedMovementAsync(factory);
            await using var firstScope = factory.Services.CreateAsyncScope();
            await using var secondScope = factory.Services.CreateAsyncScope();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var secondContext = secondScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstRepository = new FinancialMovementRepository(firstContext);
            var secondRepository = new FinancialMovementRepository(secondContext);
            var firstMovement = await firstRepository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var secondMovement = await secondRepository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var firstItem = firstMovement.Items.Single();
            var secondItem = secondMovement.Items.Single();

            firstMovement.UpdateExpenseItem(
                firstItem.Id,
                ids.CategoryId,
                "Primera",
                2m,
                5m,
                ids.UserId,
                RepositoryTestData.UtcNow.AddMinutes(1)
            );
            await firstContext.SaveChangesAsync(CancellationToken.None);
            secondMovement.UpdateExpenseItem(
                secondItem.Id,
                ids.CategoryId,
                "Obsoleta",
                3m,
                5m,
                ids.UserId,
                RepositoryTestData.UtcNow.AddMinutes(2)
            );
            var action = () => secondContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
        }

        private static async Task<SeedData> SeedAsync(MypeDbContext context)
        {
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
            await context.SaveChangesAsync(CancellationToken.None);
            return new SeedData(seed.User.Id, seed.Business.Id, category.Id);
        }

        private static async Task<MovementIds> SeedMovementAsync(MypeWebApplicationFactory factory)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var data = await SeedAsync(context);
            var movement = FinancialMovement.CreateDraft(
                data.BusinessId,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                data.UserId,
                RepositoryTestData.UtcNow
            );
            movement.AddExpenseItem(
                data.CategoryId,
                "Concepto",
                1m,
                5m,
                data.UserId,
                RepositoryTestData.UtcNow
            );
            context.FinancialMovements.Add(movement);
            await context.SaveChangesAsync(CancellationToken.None);
            return new MovementIds(movement.Id, data.BusinessId, data.CategoryId, data.UserId);
        }

        private sealed record SeedData(Guid UserId, Guid BusinessId, Guid CategoryId);

        private sealed record MovementIds(
            Guid MovementId,
            Guid BusinessId,
            Guid CategoryId,
            Guid UserId
        );
    }
}
