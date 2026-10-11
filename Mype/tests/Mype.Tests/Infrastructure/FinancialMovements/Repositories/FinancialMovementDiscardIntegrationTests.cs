using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Common.Exceptions;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Domain.MovementEvidences;
using Mype.Domain.Products;
using Mype.Infrastructure.FinancialMovements.Repositories;
using Mype.Infrastructure.MovementEvidences.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.FinancialMovements.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class FinancialMovementDiscardIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public FinancialMovementDiscardIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task Discard_Should_Persist_Movement_And_Active_Evidences_Atomically()
        {
            await using var factory = CreateFactory();
            var ids = await SeedSaleWithEvidencesAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var movementRepository = new FinancialMovementRepository(context);
            var evidenceRepository = new MovementEvidenceRepository(context);
            var movement = await movementRepository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var activeEvidences = await evidenceRepository.ListActiveTrackedByMovementAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var movementVersion = movement.Version;
            var evidenceVersions = activeEvidences.ToDictionary(
                evidence => evidence.Id,
                evidence => evidence.Version
            );
            var itemBefore = await context
                .FinancialMovementItems.AsNoTracking()
                .Where(item => item.MovementId == ids.MovementId)
                .OrderBy(item => item.Id)
                .Select(item => new
                {
                    item.Id,
                    item.IsActive,
                    item.RetiredAt,
                    item.Quantity,
                    item.UnitAmount,
                    item.SubtotalAmount,
                    item.Version,
                })
                .ToArrayAsync(CancellationToken.None);
            var nonActiveEvidenceBefore = await context
                .MovementEvidences.AsNoTracking()
                .Where(evidence =>
                    evidence.MovementId == ids.MovementId
                    && evidence.Status != MovementEvidenceStatus.Active
                )
                .Select(evidence => new
                {
                    evidence.Id,
                    evidence.Status,
                    evidence.RemovalReason,
                    evidence.MarkedForPurgeAt,
                    evidence.PurgeAfter,
                    evidence.PurgedAt,
                    evidence.Version,
                })
                .SingleAsync(CancellationToken.None);
            var totalBefore = movement.TotalAmount;
            var utcNow = RepositoryTestData.UtcNow.AddHours(2);
            var purgeAfter = utcNow.AddDays(30);

            movementRepository.SetOriginalVersion(movement, movementVersion);
            evidenceRepository.SetOriginalVersions(activeEvidences, evidenceVersions);
            movement.Discard(ids.UserId, utcNow);
            foreach (var evidence in activeEvidences)
            {
                evidence.MarkPendingPurge(
                    EvidenceRemovalReason.MovementDiscarded,
                    ids.UserId,
                    utcNow,
                    purgeAfter
                );
            }
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var persistedMovement = await context
                .FinancialMovements.AsNoTracking()
                .SingleAsync(current => current.Id == ids.MovementId, CancellationToken.None);
            var persistedActiveEvidences = await context
                .MovementEvidences.AsNoTracking()
                .Where(evidence => ids.ActiveEvidenceIds.Contains(evidence.Id))
                .ToArrayAsync(CancellationToken.None);
            var itemAfter = await context
                .FinancialMovementItems.AsNoTracking()
                .Where(item => item.MovementId == ids.MovementId)
                .OrderBy(item => item.Id)
                .Select(item => new
                {
                    item.Id,
                    item.IsActive,
                    item.RetiredAt,
                    item.Quantity,
                    item.UnitAmount,
                    item.SubtotalAmount,
                    item.Version,
                })
                .ToArrayAsync(CancellationToken.None);
            var nonActiveEvidenceAfter = await context
                .MovementEvidences.AsNoTracking()
                .Where(evidence => evidence.Id == ids.NonActiveEvidenceId)
                .Select(evidence => new
                {
                    evidence.Id,
                    evidence.Status,
                    evidence.RemovalReason,
                    evidence.MarkedForPurgeAt,
                    evidence.PurgeAfter,
                    evidence.PurgedAt,
                    evidence.Version,
                })
                .SingleAsync(CancellationToken.None);

            persistedMovement.Status.Should().Be(FinancialMovementStatus.Discarded);
            persistedMovement.DiscardedAt.Should().Be(utcNow);
            persistedMovement.UpdatedByUserId.Should().Be(ids.UserId);
            persistedMovement.TotalAmount.Should().Be(totalBefore);
            persistedActiveEvidences.Should().HaveCount(2);
            persistedActiveEvidences.Should().OnlyContain(evidence =>
                evidence.Status == MovementEvidenceStatus.PendingPurge
                && evidence.RemovalReason == EvidenceRemovalReason.MovementDiscarded
                && evidence.MarkedForPurgeAt == utcNow
                && evidence.PurgeAfter == purgeAfter
                && evidence.UpdatedByUserId == ids.UserId
            );
            itemAfter.Should().BeEquivalentTo(itemBefore, options => options.WithStrictOrdering());
            nonActiveEvidenceAfter.Should().BeEquivalentTo(nonActiveEvidenceBefore);
        }

        [Fact]
        public async Task Discard_Should_Be_Excluded_From_Draft_Queries()
        {
            await using var factory = CreateFactory();
            var ids = await SeedSaleWithEvidencesAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);
            var movement = await repository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );

            movement.Discard(ids.UserId, RepositoryTestData.UtcNow.AddHours(2));
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var detail = await repository.GetDraftByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var list = await repository.ListDraftsByBusinessAsync(
                ids.BusinessId,
                CancellationToken.None
            );

            detail.Should().BeNull();
            list.Should().NotContain(item => item.Id == ids.MovementId);
        }

        [Fact]
        public async Task Discard_Should_Reject_Stale_Movement_Version_And_Roll_Back_All_Changes()
        {
            await using var factory = CreateFactory();
            var ids = await SeedSaleWithEvidencesAsync(factory);
            await using var firstScope = factory.Services.CreateAsyncScope();
            await using var secondScope = factory.Services.CreateAsyncScope();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var secondContext = secondScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstMovementRepository = new FinancialMovementRepository(firstContext);
            var secondMovementRepository = new FinancialMovementRepository(secondContext);
            var secondEvidenceRepository = new MovementEvidenceRepository(secondContext);
            var currentMovement = await firstMovementRepository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var staleMovement = await secondMovementRepository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var activeEvidences = await secondEvidenceRepository.ListActiveTrackedByMovementAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var staleMovementVersion = staleMovement.Version;
            var evidenceVersions = activeEvidences.ToDictionary(
                evidence => evidence.Id,
                evidence => evidence.Version
            );

            currentMovement.UpdateDraftHeader(
                new DateOnly(2026, 10, 12),
                "Cambio concurrente",
                ids.UserId,
                RepositoryTestData.UtcNow.AddHours(1)
            );
            await firstContext.SaveChangesAsync(CancellationToken.None);

            secondMovementRepository.SetOriginalVersion(staleMovement, staleMovementVersion);
            secondEvidenceRepository.SetOriginalVersions(activeEvidences, evidenceVersions);
            staleMovement.Discard(ids.UserId, RepositoryTestData.UtcNow.AddHours(2));
            foreach (var evidence in activeEvidences)
            {
                evidence.MarkPendingPurge(
                    EvidenceRemovalReason.MovementDiscarded,
                    ids.UserId,
                    RepositoryTestData.UtcNow.AddHours(2),
                    RepositoryTestData.UtcNow.AddDays(30)
                );
            }

            var action = () => secondContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
            secondContext.ChangeTracker.Clear();
            var persistedMovement = await secondContext
                .FinancialMovements.AsNoTracking()
                .SingleAsync(current => current.Id == ids.MovementId, CancellationToken.None);
            var persistedEvidences = await secondContext
                .MovementEvidences.AsNoTracking()
                .Where(evidence => ids.ActiveEvidenceIds.Contains(evidence.Id))
                .ToArrayAsync(CancellationToken.None);
            persistedMovement.Status.Should().Be(FinancialMovementStatus.Draft);
            persistedMovement.DiscardedAt.Should().BeNull();
            persistedEvidences.Should().OnlyContain(
                evidence => evidence.Status == MovementEvidenceStatus.Active
            );
        }

        [Fact]
        public async Task Discard_Should_Reject_Stale_Evidence_Version_And_Roll_Back_Movement()
        {
            await using var factory = CreateFactory();
            var ids = await SeedSaleWithEvidencesAsync(factory);
            await using var firstScope = factory.Services.CreateAsyncScope();
            await using var secondScope = factory.Services.CreateAsyncScope();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var secondContext = secondScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstEvidenceRepository = new MovementEvidenceRepository(firstContext);
            var secondEvidenceRepository = new MovementEvidenceRepository(secondContext);
            var secondMovementRepository = new FinancialMovementRepository(secondContext);
            var currentEvidence = await firstEvidenceRepository.GetTrackedByIdMovementAndBusinessAsync(
                ids.ActiveEvidenceIds[0],
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var staleEvidences = await secondEvidenceRepository.ListActiveTrackedByMovementAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var movement = await secondMovementRepository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var evidenceVersions = staleEvidences.ToDictionary(
                evidence => evidence.Id,
                evidence => evidence.Version
            );

            currentEvidence.MarkPendingPurge(
                EvidenceRemovalReason.Removed,
                ids.UserId,
                RepositoryTestData.UtcNow.AddHours(1),
                RepositoryTestData.UtcNow.AddDays(30)
            );
            await firstContext.SaveChangesAsync(CancellationToken.None);

            secondMovementRepository.SetOriginalVersion(movement, movement.Version);
            secondEvidenceRepository.SetOriginalVersions(staleEvidences, evidenceVersions);
            movement.Discard(ids.UserId, RepositoryTestData.UtcNow.AddHours(2));
            foreach (var evidence in staleEvidences)
            {
                evidence.MarkPendingPurge(
                    EvidenceRemovalReason.MovementDiscarded,
                    ids.UserId,
                    RepositoryTestData.UtcNow.AddHours(2),
                    RepositoryTestData.UtcNow.AddDays(31)
                );
            }

            var action = () => secondContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
            secondContext.ChangeTracker.Clear();
            var persistedMovement = await secondContext
                .FinancialMovements.AsNoTracking()
                .SingleAsync(current => current.Id == ids.MovementId, CancellationToken.None);
            persistedMovement.Status.Should().Be(FinancialMovementStatus.Draft);
            persistedMovement.DiscardedAt.Should().BeNull();
        }

        [Fact]
        public async Task Confirm_And_Discard_Race_Should_Persist_Only_One_Transition()
        {
            await using var factory = CreateFactory();
            var ids = await SeedSaleWithEvidencesAsync(factory);
            await using var confirmScope = factory.Services.CreateAsyncScope();
            await using var discardScope = factory.Services.CreateAsyncScope();
            var confirmContext = confirmScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var discardContext = discardScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var confirmRepository = new FinancialMovementRepository(confirmContext);
            var discardRepository = new FinancialMovementRepository(discardContext);
            var movementToConfirm = await confirmRepository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var movementToDiscard = await discardRepository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var confirmItemVersions = movementToConfirm.Items.ToDictionary(
                item => item.Id,
                item => item.Version
            );

            confirmRepository.SetOriginalVersions(
                movementToConfirm,
                movementToConfirm.Version,
                confirmItemVersions
            );
            discardRepository.SetOriginalVersion(movementToDiscard, movementToDiscard.Version);
            movementToConfirm.Confirm(ids.UserId, RepositoryTestData.UtcNow.AddHours(1));
            movementToDiscard.Discard(ids.UserId, RepositoryTestData.UtcNow.AddHours(1));
            await confirmContext.SaveChangesAsync(CancellationToken.None);

            var action = () => discardContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
            discardContext.ChangeTracker.Clear();
            var persisted = await discardContext
                .FinancialMovements.AsNoTracking()
                .SingleAsync(current => current.Id == ids.MovementId, CancellationToken.None);
            persisted.Status.Should().Be(FinancialMovementStatus.Confirmed);
            persisted.DiscardedAt.Should().BeNull();
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static async Task<SeedIds> SeedSaleWithEvidencesAsync(
            MypeWebApplicationFactory factory
        )
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var category = Category.CreateDefault(
                seed.Business.Id,
                CategoryType.Sale,
                "Productos",
                $"PRODUCTOS {seed.Suffix}".ToUpperInvariant(),
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            var product = Product.Create(
                seed.Business.Id,
                category.Id,
                "Gaseosa",
                $"GASEOSA {seed.Suffix}".ToUpperInvariant(),
                4m,
                2m,
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            var movement = FinancialMovement.CreateDraft(
                seed.Business.Id,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 11),
                null,
                "PEN",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            movement.AddSaleItem(product, 2m, 4m, seed.User.Id, RepositoryTestData.UtcNow);
            var retired = movement.AddSaleItem(
                product,
                1m,
                4m,
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            movement.RetireItem(retired.Id, seed.User.Id, RepositoryTestData.UtcNow.AddMinutes(1));

            context.Categories.Add(category);
            context.Products.Add(product);
            context.FinancialMovements.Add(movement);
            await context.SaveChangesAsync(CancellationToken.None);

            var first = CreateEvidence(
                seed.Business.Id,
                movement.Id,
                seed.User.Id,
                "first.pdf",
                RepositoryTestData.UtcNow.AddMinutes(2)
            );
            var second = CreateEvidence(
                seed.Business.Id,
                movement.Id,
                seed.User.Id,
                "second.pdf",
                RepositoryTestData.UtcNow.AddMinutes(3)
            );
            var nonActive = CreateEvidence(
                seed.Business.Id,
                movement.Id,
                seed.User.Id,
                "removed.pdf",
                RepositoryTestData.UtcNow.AddMinutes(4)
            );
            nonActive.MarkPendingPurge(
                EvidenceRemovalReason.Removed,
                seed.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(5),
                RepositoryTestData.UtcNow.AddDays(30)
            );
            context.MovementEvidences.AddRange(first, second, nonActive);
            await context.SaveChangesAsync(CancellationToken.None);

            return new(
                movement.Id,
                seed.Business.Id,
                seed.User.Id,
                [first.Id, second.Id],
                nonActive.Id
            );
        }

        private static MovementEvidence CreateEvidence(
            Guid businessId,
            Guid movementId,
            Guid userId,
            string fileName,
            DateTimeOffset uploadedAt
        )
        {
            var id = Guid.NewGuid();
            return MovementEvidence.CreateActive(
                id,
                businessId,
                movementId,
                null,
                "AzureBlob",
                "evidences",
                $"businesses/{businessId:N}/movements/{movementId:N}/evidences/{id:N}/{fileName}",
                fileName,
                "application/pdf",
                128,
                userId,
                uploadedAt
            );
        }

        private sealed record SeedIds(
            Guid MovementId,
            Guid BusinessId,
            Guid UserId,
            IReadOnlyList<Guid> ActiveEvidenceIds,
            Guid NonActiveEvidenceId
        );
    }
}
