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
    public sealed class FinancialMovementConfirmationIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public FinancialMovementConfirmationIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Reject_Stale_Movement_Version_And_Roll_Back_Confirmation()
        {
            await using var factory = CreateFactory();
            var ids = await SeedSaleAsync(factory);
            await using var firstScope = factory.Services.CreateAsyncScope();
            await using var secondScope = factory.Services.CreateAsyncScope();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var secondContext = secondScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstRepository = new FinancialMovementRepository(firstContext);
            var secondRepository = new FinancialMovementRepository(secondContext);
            var currentMovement = await firstRepository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var staleMovement = await secondRepository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var staleMovementVersion = staleMovement.Version;
            var staleItemVersions = staleMovement.Items.ToDictionary(
                item => item.Id,
                item => item.Version
            );

            currentMovement.UpdateDraftHeader(
                new DateOnly(2026, 10, 11),
                "Actualización concurrente",
                ids.UserId,
                RepositoryTestData.UtcNow.AddHours(1)
            );
            await firstContext.SaveChangesAsync(CancellationToken.None);

            secondRepository.SetOriginalVersions(
                staleMovement,
                staleMovementVersion,
                staleItemVersions
            );
            staleMovement.Confirm(ids.UserId, RepositoryTestData.UtcNow.AddHours(2));

            var action = () => secondContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
            exception.Which.ErrorType.Should().Be(ApplicationErrorType.Conflict);

            secondContext.ChangeTracker.Clear();
            var persisted = await secondContext
                .FinancialMovements.AsNoTracking()
                .SingleAsync(movement => movement.Id == ids.MovementId, CancellationToken.None);
            persisted.Status.Should().Be(FinancialMovementStatus.Draft);
            persisted.ConfirmedByUserId.Should().BeNull();
            persisted.ConfirmedAt.Should().BeNull();
        }

        [Fact]
        public async Task Confirm_Should_Leave_All_Active_Evidences_Unchanged()
        {
            await using var factory = CreateFactory();
            var ids = await SeedSaleWithTwoEvidencesAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);
            var movement = await repository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var evidenceBefore = await context
                .MovementEvidences.AsNoTracking()
                .Where(evidence => evidence.MovementId == ids.MovementId)
                .OrderBy(evidence => evidence.Id)
                .Select(evidence => new
                {
                    evidence.Id,
                    evidence.Status,
                    evidence.ReplacesEvidenceId,
                    evidence.StorageProvider,
                    evidence.StorageContainer,
                    evidence.StorageObjectKey,
                    evidence.OriginalFileName,
                    evidence.MimeType,
                    evidence.SizeBytes,
                    evidence.UploadedByUserId,
                    evidence.UploadedAt,
                    evidence.Version,
                })
                .ToArrayAsync(CancellationToken.None);
            var itemVersions = movement.Items.ToDictionary(item => item.Id, item => item.Version);

            repository.SetOriginalVersions(movement, movement.Version, itemVersions);
            movement.Confirm(ids.UserId, RepositoryTestData.UtcNow.AddHours(1));
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var evidenceAfter = await context
                .MovementEvidences.AsNoTracking()
                .Where(evidence => evidence.MovementId == ids.MovementId)
                .OrderBy(evidence => evidence.Id)
                .Select(evidence => new
                {
                    evidence.Id,
                    evidence.Status,
                    evidence.ReplacesEvidenceId,
                    evidence.StorageProvider,
                    evidence.StorageContainer,
                    evidence.StorageObjectKey,
                    evidence.OriginalFileName,
                    evidence.MimeType,
                    evidence.SizeBytes,
                    evidence.UploadedByUserId,
                    evidence.UploadedAt,
                    evidence.Version,
                })
                .ToArrayAsync(CancellationToken.None);
            var confirmedMovement = await context
                .FinancialMovements.AsNoTracking()
                .SingleAsync(current => current.Id == ids.MovementId, CancellationToken.None);

            evidenceBefore.Should().HaveCount(2);
            evidenceAfter
                .Should()
                .BeEquivalentTo(evidenceBefore, options => options.WithStrictOrdering());
            evidenceAfter
                .Should()
                .OnlyContain(evidence => evidence.Status == MovementEvidenceStatus.Active);
            confirmedMovement.Status.Should().Be(FinancialMovementStatus.Confirmed);
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static async Task<SeedIds> SeedSaleAsync(MypeWebApplicationFactory factory)
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
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            movement.AddSaleItem(product, 2m, 4m, seed.User.Id, RepositoryTestData.UtcNow);

            context.Categories.Add(category);
            context.Products.Add(product);
            context.FinancialMovements.Add(movement);
            await context.SaveChangesAsync(CancellationToken.None);

            return new(movement.Id, seed.Business.Id, seed.User.Id);
        }

        private static async Task<SeedIds> SeedSaleWithTwoEvidencesAsync(
            MypeWebApplicationFactory factory
        )
        {
            var ids = await SeedSaleAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            var first = MovementEvidence.CreateActive(
                firstId,
                ids.BusinessId,
                ids.MovementId,
                null,
                "AzureBlob",
                "evidences",
                $"businesses/{ids.BusinessId:N}/movements/{ids.MovementId:N}/evidences/{firstId:N}/first.pdf",
                "first.pdf",
                "application/pdf",
                128,
                ids.UserId,
                RepositoryTestData.UtcNow.AddMinutes(1)
            );
            var second = MovementEvidence.CreateActive(
                secondId,
                ids.BusinessId,
                ids.MovementId,
                null,
                "AzureBlob",
                "evidences",
                $"businesses/{ids.BusinessId:N}/movements/{ids.MovementId:N}/evidences/{secondId:N}/second.png",
                "second.png",
                "image/png",
                256,
                ids.UserId,
                RepositoryTestData.UtcNow.AddMinutes(2)
            );

            context.MovementEvidences.AddRange(first, second);
            await context.SaveChangesAsync(CancellationToken.None);
            return ids;
        }

        private sealed record SeedIds(Guid MovementId, Guid BusinessId, Guid UserId);
    }
}
