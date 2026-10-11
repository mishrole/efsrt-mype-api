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
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Domain.MovementEvidences;
using Mype.Domain.Products;
using Mype.Infrastructure.FinancialMovements.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.FinancialMovements.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class FinancialMovementCancellationIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public FinancialMovementCancellationIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task Cancel_Should_Persist_Transition_And_Preserve_Items_And_Evidences()
        {
            await using var factory = CreateFactory();
            var ids = await SeedConfirmedSaleAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);
            var movement = await repository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var itemsBefore = await context
                .FinancialMovementItems.AsNoTracking()
                .Where(item => item.MovementId == ids.MovementId)
                .OrderBy(item => item.Id)
                .Select(item => new
                {
                    item.Id,
                    item.CategoryId,
                    item.ProductId,
                    item.Description,
                    item.Quantity,
                    item.UnitAmount,
                    item.UnitCostSnapshot,
                    item.SubtotalAmount,
                    item.IsActive,
                    item.RetiredAt,
                    item.Version,
                })
                .ToArrayAsync();
            var evidencesBefore = await context
                .MovementEvidences.AsNoTracking()
                .Where(evidence => evidence.MovementId == ids.MovementId)
                .OrderBy(evidence => evidence.Id)
                .Select(evidence => new
                {
                    evidence.Id,
                    evidence.Status,
                    evidence.StorageObjectKey,
                    evidence.OriginalFileName,
                    evidence.UpdatedAt,
                    evidence.Version,
                })
                .ToArrayAsync();
            var total = movement.TotalAmount;
            var confirmedAt = movement.ConfirmedAt;
            var confirmedBy = movement.ConfirmedByUserId;
            var reason = new string('a', FinancialMovementConstraints.CancellationReasonMinLength);
            var cancelledAt = RepositoryTestData.UtcNow.AddHours(2);

            repository.SetOriginalVersion(movement, movement.Version);
            movement.Cancel(reason, ids.UserId, cancelledAt);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var persisted = await context
                .FinancialMovements.AsNoTracking()
                .SingleAsync(current => current.Id == ids.MovementId);
            var itemsAfter = await context
                .FinancialMovementItems.AsNoTracking()
                .Where(item => item.MovementId == ids.MovementId)
                .OrderBy(item => item.Id)
                .Select(item => new
                {
                    item.Id,
                    item.CategoryId,
                    item.ProductId,
                    item.Description,
                    item.Quantity,
                    item.UnitAmount,
                    item.UnitCostSnapshot,
                    item.SubtotalAmount,
                    item.IsActive,
                    item.RetiredAt,
                    item.Version,
                })
                .ToArrayAsync();
            var evidencesAfter = await context
                .MovementEvidences.AsNoTracking()
                .Where(evidence => evidence.MovementId == ids.MovementId)
                .OrderBy(evidence => evidence.Id)
                .Select(evidence => new
                {
                    evidence.Id,
                    evidence.Status,
                    evidence.StorageObjectKey,
                    evidence.OriginalFileName,
                    evidence.UpdatedAt,
                    evidence.Version,
                })
                .ToArrayAsync();

            persisted.Status.Should().Be(FinancialMovementStatus.Cancelled);
            persisted.CancellationReason.Should().Be(reason);
            persisted.CancelledAt.Should().Be(cancelledAt);
            persisted.CancelledByUserId.Should().Be(ids.UserId);
            persisted.ConfirmedAt.Should().Be(confirmedAt);
            persisted.ConfirmedByUserId.Should().Be(confirmedBy);
            persisted.TotalAmount.Should().Be(total);
            itemsAfter
                .Should()
                .BeEquivalentTo(itemsBefore, options => options.WithStrictOrdering());
            evidencesAfter
                .Should()
                .BeEquivalentTo(evidencesBefore, options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task Cancel_Should_Reject_Stale_Version_And_Preserve_First_Cancellation()
        {
            await using var factory = CreateFactory();
            var ids = await SeedConfirmedSaleAsync(factory);
            await using var firstScope = factory.Services.CreateAsyncScope();
            await using var secondScope = factory.Services.CreateAsyncScope();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var secondContext = secondScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstRepository = new FinancialMovementRepository(firstContext);
            var secondRepository = new FinancialMovementRepository(secondContext);
            var first = await firstRepository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var stale = await secondRepository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var staleVersion = stale.Version;
            var firstReason = new string(
                'a',
                FinancialMovementConstraints.CancellationReasonMinLength
            );
            var secondReason = new string(
                'b',
                FinancialMovementConstraints.CancellationReasonMinLength
            );

            firstRepository.SetOriginalVersion(first, first.Version);
            first.Cancel(firstReason, ids.UserId, RepositoryTestData.UtcNow.AddHours(1));
            await firstContext.SaveChangesAsync(CancellationToken.None);

            secondRepository.SetOriginalVersion(stale, staleVersion);
            stale.Cancel(secondReason, ids.UserId, RepositoryTestData.UtcNow.AddHours(2));
            var action = () => secondContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
            secondContext.ChangeTracker.Clear();
            var persisted = await secondContext
                .FinancialMovements.AsNoTracking()
                .SingleAsync(current => current.Id == ids.MovementId);
            persisted.Status.Should().Be(FinancialMovementStatus.Cancelled);
            persisted.CancellationReason.Should().Be(firstReason);
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static async Task<SeedIds> SeedConfirmedSaleAsync(MypeWebApplicationFactory factory)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var category = Category.CreateDefault(
                seed.Business.Id,
                CategoryType.Sale,
                "Ventas",
                $"VENTAS {seed.Suffix}".ToUpperInvariant(),
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            var product = Product.Create(
                seed.Business.Id,
                category.Id,
                "Producto",
                $"PRODUCTO {seed.Suffix}".ToUpperInvariant(),
                5m,
                2m,
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            var movement = FinancialMovement.CreateDraft(
                seed.Business.Id,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 11),
                "Venta",
                "PEN",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            movement.AddSaleItem(product, 2m, 5m, seed.User.Id, RepositoryTestData.UtcNow);
            var retired = movement.AddSaleItem(
                product,
                1m,
                5m,
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            movement.RetireItem(retired.Id, seed.User.Id, RepositoryTestData.UtcNow.AddMinutes(1));
            movement.Confirm(seed.User.Id, RepositoryTestData.UtcNow.AddMinutes(2));
            context.Categories.Add(category);
            context.Products.Add(product);
            context.FinancialMovements.Add(movement);
            await context.SaveChangesAsync();

            var evidenceId = Guid.NewGuid();
            context.MovementEvidences.Add(
                MovementEvidence.CreateActive(
                    evidenceId,
                    seed.Business.Id,
                    movement.Id,
                    null,
                    "AzureBlob",
                    "evidences",
                    $"history/{movement.Id:N}/{evidenceId:N}/evidence.pdf",
                    "evidence.pdf",
                    "application/pdf",
                    128,
                    seed.User.Id,
                    RepositoryTestData.UtcNow.AddMinutes(3)
                )
            );
            await context.SaveChangesAsync();
            return new(movement.Id, seed.Business.Id, seed.User.Id);
        }

        private sealed record SeedIds(Guid MovementId, Guid BusinessId, Guid UserId);
    }
}
