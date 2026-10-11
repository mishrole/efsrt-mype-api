using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Common.Exceptions;
using Mype.Domain.FinancialMovements;
using Mype.Domain.MovementEvidences;
using Mype.Infrastructure.FinancialMovements.Repositories;
using Mype.Infrastructure.MovementEvidences.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.MovementEvidences.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class MovementEvidenceRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public MovementEvidenceRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task AddAsync_And_GetTracked_Should_Persist_And_Load_Evidence()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await SeedMovementAsync(context);
            var repository = new MovementEvidenceRepository(context);
            var evidence = CreateEvidence(seed, RepositoryTestData.UtcNow.AddMinutes(1));

            await repository.AddAsync(evidence, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var persisted = await repository.GetTrackedByIdMovementAndBusinessAsync(
                evidence.Id,
                seed.Movement.Id,
                seed.Business.Id,
                CancellationToken.None
            );

            persisted.Should().NotBeNull();
            persisted.StorageObjectKey.Should().Be(evidence.StorageObjectKey);
            persisted.Status.Should().Be(MovementEvidenceStatus.Active);
            context.Entry(persisted).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetTracked_Should_Isolate_Business_And_Movement()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await SeedMovementAsync(context);
            var repository = new MovementEvidenceRepository(context);
            var evidence = CreateEvidence(seed, RepositoryTestData.UtcNow.AddMinutes(1));
            await repository.AddAsync(evidence, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var otherBusiness = await repository.GetTrackedByIdMovementAndBusinessAsync(
                evidence.Id,
                seed.Movement.Id,
                Guid.NewGuid(),
                CancellationToken.None
            );
            var otherMovement = await repository.GetTrackedByIdMovementAndBusinessAsync(
                evidence.Id,
                Guid.NewGuid(),
                seed.Business.Id,
                CancellationToken.None
            );

            otherBusiness.Should().BeNull();
            otherMovement.Should().BeNull();
        }

        [Fact]
        public async Task ListActive_Should_Return_Multiple_Active_Evidences_In_Descending_Upload_Order()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await SeedMovementAsync(context);
            var repository = new MovementEvidenceRepository(context);
            var older = CreateEvidence(seed, RepositoryTestData.UtcNow.AddMinutes(1));
            var newer = CreateEvidence(seed, RepositoryTestData.UtcNow.AddMinutes(2));

            await repository.AddAsync(older, CancellationToken.None);
            await repository.AddAsync(newer, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var result = await repository.ListActiveByMovementAndBusinessAsync(
                seed.Movement.Id,
                seed.Business.Id,
                CancellationToken.None
            );

            result.Select(current => current.Id).Should().Equal(newer.Id, older.Id);
            context.ChangeTracker.Entries<MovementEvidence>().Should().BeEmpty();
        }

        [Fact]
        public async Task ListActive_Should_Exclude_PendingPurge_And_Isolate_Movement_And_Business()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstSeed = await SeedMovementAsync(context);
            var secondMovement = CreateMovement(firstSeed.Business.Id, firstSeed.User.Id);
            context.FinancialMovements.Add(secondMovement);
            var secondBusiness = await RepositoryTestData.SeedBusinessAsync(context);
            var thirdMovement = CreateMovement(secondBusiness.Business.Id, secondBusiness.User.Id);
            context.FinancialMovements.Add(thirdMovement);
            await context.SaveChangesAsync(CancellationToken.None);
            var repository = new MovementEvidenceRepository(context);
            var active = CreateEvidence(firstSeed, RepositoryTestData.UtcNow.AddMinutes(1));
            var pending = CreateEvidence(firstSeed, RepositoryTestData.UtcNow.AddMinutes(2));
            pending.MarkPendingPurge(
                EvidenceRemovalReason.Removed,
                firstSeed.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(3),
                RepositoryTestData.UtcNow.AddDays(30)
            );
            var otherMovementEvidence = CreateEvidence(
                new MovementSeed(firstSeed.User, firstSeed.Business, secondMovement),
                RepositoryTestData.UtcNow.AddMinutes(4)
            );
            var otherBusinessEvidence = CreateEvidence(
                new MovementSeed(secondBusiness.User, secondBusiness.Business, thirdMovement),
                RepositoryTestData.UtcNow.AddMinutes(5)
            );

            await repository.AddAsync(active, CancellationToken.None);
            await repository.AddAsync(pending, CancellationToken.None);
            await repository.AddAsync(otherMovementEvidence, CancellationToken.None);
            await repository.AddAsync(otherBusinessEvidence, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var result = await repository.ListActiveByMovementAndBusinessAsync(
                firstSeed.Movement.Id,
                firstSeed.Business.Id,
                CancellationToken.None
            );

            result.Should().ContainSingle(current => current.Id == active.Id);
            result
                .Should()
                .NotContain(current =>
                    current.Id == pending.Id
                    || current.Id == otherMovementEvidence.Id
                    || current.Id == otherBusinessEvidence.Id
                );
        }

        [Fact]
        public async Task SetOriginalVersion_Should_Set_Evidence_Original_Value()
        {
            await using var factory = CreateFactory();
            var ids = await SeedEvidenceAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new MovementEvidenceRepository(context);
            var evidence = await repository.GetTrackedByIdMovementAndBusinessAsync(
                ids.EvidenceId,
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );

            repository.SetOriginalVersion(evidence, 37);

            context
                .Entry(evidence)
                .Property(current => current.Version)
                .OriginalValue.Should()
                .Be(37);
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Translate_Stale_Evidence_Version()
        {
            await using var factory = CreateFactory();
            var ids = await SeedEvidenceAsync(factory);
            await using var firstScope = factory.Services.CreateAsyncScope();
            await using var secondScope = factory.Services.CreateAsyncScope();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var secondContext = secondScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstRepository = new MovementEvidenceRepository(firstContext);
            var secondRepository = new MovementEvidenceRepository(secondContext);
            var firstEvidence = await firstRepository.GetTrackedByIdMovementAndBusinessAsync(
                ids.EvidenceId,
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var secondEvidence = await secondRepository.GetTrackedByIdMovementAndBusinessAsync(
                ids.EvidenceId,
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );

            firstEvidence.MarkPendingPurge(
                EvidenceRemovalReason.Removed,
                ids.UserId,
                RepositoryTestData.UtcNow.AddHours(1),
                RepositoryTestData.UtcNow.AddDays(30)
            );
            await firstContext.SaveChangesAsync(CancellationToken.None);

            secondEvidence.MarkPendingPurge(
                EvidenceRemovalReason.Replaced,
                ids.UserId,
                RepositoryTestData.UtcNow.AddHours(2),
                RepositoryTestData.UtcNow.AddDays(31)
            );
            var action = () => secondContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
            exception.Which.ErrorType.Should().Be(ApplicationErrorType.Conflict);
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static FinancialMovement CreateMovement(Guid businessId, Guid userId) =>
            FinancialMovement.CreateDraft(
                businessId,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 10),
                "Gasto con evidencia",
                "PEN",
                userId,
                RepositoryTestData.UtcNow
            );

        private static MovementEvidence CreateEvidence(MovementSeed seed, DateTimeOffset uploadedAt)
        {
            var evidenceId = Guid.NewGuid();
            return MovementEvidence.CreateActive(
                evidenceId,
                seed.Business.Id,
                seed.Movement.Id,
                null,
                "AzureBlob",
                "evidences",
                $"businesses/{seed.Business.Id:N}/movements/{seed.Movement.Id:N}/evidences/{evidenceId:N}/{Guid.NewGuid():N}.pdf",
                "receipt.pdf",
                "application/pdf",
                128,
                seed.User.Id,
                uploadedAt
            );
        }

        private static async Task<MovementSeed> SeedMovementAsync(MypeDbContext context)
        {
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var movement = CreateMovement(seed.Business.Id, seed.User.Id);
            var repository = new FinancialMovementRepository(context);
            await repository.AddAsync(movement, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            return new(seed.User, seed.Business, movement);
        }

        private static async Task<EvidenceIds> SeedEvidenceAsync(MypeWebApplicationFactory factory)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await SeedMovementAsync(context);
            var repository = new MovementEvidenceRepository(context);
            var evidence = CreateEvidence(seed, RepositoryTestData.UtcNow.AddMinutes(1));
            await repository.AddAsync(evidence, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            return new(evidence.Id, seed.Movement.Id, seed.Business.Id, seed.User.Id);
        }

        private sealed record MovementSeed(
            Mype.Domain.Users.User User,
            Mype.Domain.Businesses.Business Business,
            FinancialMovement Movement
        );

        private sealed record EvidenceIds(
            Guid EvidenceId,
            Guid MovementId,
            Guid BusinessId,
            Guid UserId
        );
    }
}
