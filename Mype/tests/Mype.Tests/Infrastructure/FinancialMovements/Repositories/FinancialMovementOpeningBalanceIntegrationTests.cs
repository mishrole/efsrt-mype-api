using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Common.Exceptions;
using Mype.Domain.FinancialMovements;
using Mype.Infrastructure.FinancialMovements.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.FinancialMovements.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class FinancialMovementOpeningBalanceIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public FinancialMovementOpeningBalanceIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Theory]
        [InlineData(2500.00)]
        [InlineData(0.00)]
        [InlineData(-500.00)]
        public async Task OpeningBalance_Should_Persist_Signed_Amount_And_Query_Current(
            decimal amount
        )
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var repository = new FinancialMovementRepository(context);
            var openingDate = new DateOnly(2026, 10, 1);
            var opening = FinancialMovement.CreateOpeningBalance(
                seed.Business.Id,
                openingDate,
                amount,
                "PEN",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );

            await repository.AddAsync(opening, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var tracked = await repository.GetCurrentOpeningBalanceTrackedAsync(
                seed.Business.Id,
                CancellationToken.None
            );
            tracked.Should().NotBeNull();
            tracked.TotalAmount.Should().Be(amount);
            tracked.MovementDate.Should().Be(openingDate);
            tracked.Status.Should().Be(FinancialMovementStatus.Confirmed);
            tracked.Type.Should().Be(FinancialMovementType.OpeningBalance);
            tracked.IsCurrentOpeningBalance.Should().BeTrue();
            tracked.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetCurrentOpeningBalance_Should_Isolate_Business_And_Return_NoTracking_Projection()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var first = await RepositoryTestData.SeedBusinessAsync(context);
            var second = await RepositoryTestData.SeedBusinessAsync(context);
            var repository = new FinancialMovementRepository(context);
            var opening = FinancialMovement.CreateOpeningBalance(
                first.Business.Id,
                new DateOnly(2026, 10, 1),
                900m,
                "PEN",
                first.User.Id,
                RepositoryTestData.UtcNow
            );
            await repository.AddAsync(opening, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var own = await repository.GetCurrentOpeningBalanceAsync(
                first.Business.Id,
                CancellationToken.None
            );
            var foreign = await repository.GetCurrentOpeningBalanceAsync(
                second.Business.Id,
                CancellationToken.None
            );

            own.Should().NotBeNull();
            own.Id.Should().Be(opening.Id);
            foreign.Should().BeNull();
            context.ChangeTracker.Entries().Should().BeEmpty();
        }

        [Fact]
        public async Task Replacement_Should_Persist_New_Current_And_Preserve_Previous()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var repository = new FinancialMovementRepository(context);
            var date = new DateOnly(2026, 10, 1);
            var original = FinancialMovement.CreateOpeningBalance(
                seed.Business.Id,
                date,
                1000m,
                "PEN",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            await repository.AddAsync(original, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);

            repository.SetOriginalVersion(original, original.Version);
            original.SupersedeOpeningBalance(seed.User.Id, RepositoryTestData.UtcNow.AddMinutes(1));
            var replacement = FinancialMovement.CreateOpeningBalance(
                seed.Business.Id,
                date,
                -250m,
                "PEN",
                seed.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(1),
                original.Id,
                "Conciliación inicial"
            );
            await repository.AddAsync(replacement, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var storedOriginal = await context
                .FinancialMovements.AsNoTracking()
                .SingleAsync(movement => movement.Id == original.Id);
            var current = await repository.GetCurrentOpeningBalanceTrackedAsync(
                seed.Business.Id,
                CancellationToken.None
            );
            storedOriginal.IsCurrentOpeningBalance.Should().BeFalse();
            current.Id.Should().Be(replacement.Id);
            current.ReplacesMovementId.Should().Be(original.Id);
            current.ReplacementReason.Should().Be("Conciliación inicial");
            current.TotalAmount.Should().Be(-250m);
            current.MovementDate.Should().Be(date);
        }

        [Fact]
        public async Task Unique_Filtered_Index_Should_Reject_Two_Current_Openings_For_Same_Business()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var repository = new FinancialMovementRepository(context);
            await repository.AddAsync(
                FinancialMovement.CreateOpeningBalance(
                    seed.Business.Id,
                    new DateOnly(2026, 10, 1),
                    100m,
                    "PEN",
                    seed.User.Id,
                    RepositoryTestData.UtcNow
                ),
                CancellationToken.None
            );
            await repository.AddAsync(
                FinancialMovement.CreateOpeningBalance(
                    seed.Business.Id,
                    new DateOnly(2026, 10, 1),
                    200m,
                    "PEN",
                    seed.User.Id,
                    RepositoryTestData.UtcNow
                ),
                CancellationToken.None
            );

            var action = () => context.SaveChangesAsync(CancellationToken.None);

            await action.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task Replacement_Should_Reject_Stale_OpeningBalance_Version()
        {
            await using var factory = CreateFactory();
            await using var seedScope = factory.Services.CreateAsyncScope();
            var seedContext = seedScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(seedContext);
            var opening = FinancialMovement.CreateOpeningBalance(
                seed.Business.Id,
                new DateOnly(2026, 10, 1),
                1000m,
                "PEN",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            seedContext.FinancialMovements.Add(opening);
            await seedContext.SaveChangesAsync(CancellationToken.None);

            await using var firstScope = factory.Services.CreateAsyncScope();
            await using var staleScope = factory.Services.CreateAsyncScope();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var staleContext = staleScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstRepository = new FinancialMovementRepository(firstContext);
            var staleRepository = new FinancialMovementRepository(staleContext);
            var first = await firstRepository.GetCurrentOpeningBalanceTrackedAsync(
                seed.Business.Id,
                CancellationToken.None
            );
            var stale = await staleRepository.GetCurrentOpeningBalanceTrackedAsync(
                seed.Business.Id,
                CancellationToken.None
            );
            var staleVersion = stale.Version;

            firstRepository.SetOriginalVersion(first, first.Version);
            first.SupersedeOpeningBalance(seed.User.Id, RepositoryTestData.UtcNow.AddMinutes(1));
            await firstContext.SaveChangesAsync(CancellationToken.None);

            staleRepository.SetOriginalVersion(stale, staleVersion);
            stale.SupersedeOpeningBalance(seed.User.Id, RepositoryTestData.UtcNow.AddMinutes(2));
            var action = () => staleContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);
    }
}
