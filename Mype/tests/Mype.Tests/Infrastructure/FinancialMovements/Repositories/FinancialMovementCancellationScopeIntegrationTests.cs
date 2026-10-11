using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Infrastructure.FinancialMovements.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.FinancialMovements.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class FinancialMovementCancellationScopeIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public FinancialMovementCancellationScopeIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task Cancel_Confirmed_Expense_Should_Persist_Without_Modifying_Items()
        {
            await using var factory = CreateFactory();
            var ids = await SeedConfirmedExpenseAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);
            var movement = await repository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var itemBefore = await context
                .FinancialMovementItems.AsNoTracking()
                .SingleAsync(item => item.MovementId == ids.MovementId);
            var total = movement.TotalAmount;
            var confirmedAt = movement.ConfirmedAt;
            var reason = new string('a', FinancialMovementConstraints.CancellationReasonMinLength);
            var cancelledAt = RepositoryTestData.UtcNow.AddHours(2);

            repository.SetOriginalVersion(movement, movement.Version);
            movement.Cancel(reason, ids.UserId, cancelledAt);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var persisted = await context
                .FinancialMovements.AsNoTracking()
                .SingleAsync(current => current.Id == ids.MovementId);
            var itemAfter = await context
                .FinancialMovementItems.AsNoTracking()
                .SingleAsync(item => item.MovementId == ids.MovementId);

            persisted.Type.Should().Be(FinancialMovementType.Expense);
            persisted.Status.Should().Be(FinancialMovementStatus.Cancelled);
            persisted.TotalAmount.Should().Be(total);
            persisted.ConfirmedAt.Should().Be(confirmedAt);
            persisted.CancellationReason.Should().Be(reason);
            persisted.CancelledAt.Should().Be(cancelledAt);
            itemAfter.Id.Should().Be(itemBefore.Id);
            itemAfter.CategoryId.Should().Be(itemBefore.CategoryId);
            itemAfter.ProductId.Should().BeNull();
            itemAfter.Description.Should().Be(itemBefore.Description);
            itemAfter.Quantity.Should().Be(itemBefore.Quantity);
            itemAfter.UnitAmount.Should().Be(itemBefore.UnitAmount);
            itemAfter.SubtotalAmount.Should().Be(itemBefore.SubtotalAmount);
            itemAfter.IsActive.Should().Be(itemBefore.IsActive);
            itemAfter.Version.Should().Be(itemBefore.Version);
        }

        [Fact]
        public async Task Cancel_Should_Appear_In_History_With_Cancellation_Audit()
        {
            await using var factory = CreateFactory();
            var ids = await SeedConfirmedExpenseAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);
            var movement = await repository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var reason = new string('b', FinancialMovementConstraints.CancellationReasonMinLength);
            var cancelledAt = RepositoryTestData.UtcNow.AddHours(2);

            repository.SetOriginalVersion(movement, movement.Version);
            movement.Cancel(reason, ids.UserId, cancelledAt);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var history = await repository.GetHistoryDetailByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );

            history.Should().NotBeNull();
            history.Status.Should().Be(FinancialMovementStatus.Cancelled);
            history.ConfirmedByUserId.Should().Be(ids.UserId);
            history.ConfirmedAt.Should().Be(ids.ConfirmedAt);
            history.CancelledByUserId.Should().Be(ids.UserId);
            history.CancelledAt.Should().Be(cancelledAt);
            history.CancellationReason.Should().Be(reason);
            history.Items.Should().ContainSingle();
            history.Evidences.Should().BeEmpty();
            context.ChangeTracker.Entries().Should().BeEmpty();
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static async Task<SeedIds> SeedConfirmedExpenseAsync(
            MypeWebApplicationFactory factory
        )
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
            var movement = FinancialMovement.CreateDraft(
                seed.Business.Id,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 11),
                "Gasto confirmado",
                "PEN",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            movement.AddExpenseItem(
                category.Id,
                "Servicio",
                2m,
                6m,
                seed.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(1)
            );
            var confirmedAt = RepositoryTestData.UtcNow.AddMinutes(2);
            movement.Confirm(seed.User.Id, confirmedAt);
            context.Categories.Add(category);
            context.FinancialMovements.Add(movement);
            await context.SaveChangesAsync(CancellationToken.None);
            return new(movement.Id, seed.Business.Id, seed.User.Id, confirmedAt);
        }

        private sealed record SeedIds(
            Guid MovementId,
            Guid BusinessId,
            Guid UserId,
            DateTimeOffset ConfirmedAt
        );
    }
}
