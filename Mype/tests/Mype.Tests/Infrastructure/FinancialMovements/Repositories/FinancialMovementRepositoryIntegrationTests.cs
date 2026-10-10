using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Common.Exceptions;
using Mype.Domain.Businesses;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Products;
using Mype.Domain.Users;
using Mype.Infrastructure.FinancialMovements.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using Mype.Tests.Integration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Infrastructure.FinancialMovements.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class FinancialMovementRepositoryIntegrationTests
    {
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        private readonly PostgreSqlFixture _database;

        public FinancialMovementRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task AddAsync_Should_Persist_Movement_And_Item_Atomically()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);
            var data = await SeedDependenciesAsync(context);
            var movement = CreateMovement(data.Business.Id, data.User.Id);
            var item = movement.AddSaleItem(
                data.Product,
                3.5000m,
                4.20m,
                data.User.Id,
                UtcNow.AddMinutes(1)
            );

            await repository.AddAsync(movement, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var persisted = await context
                .FinancialMovements.AsNoTracking()
                .Include(current => current.Items)
                .SingleAsync(current => current.Id == movement.Id);

            persisted.TotalAmount.Should().Be(14.70m);
            persisted.Items.Should().ContainSingle();
            persisted.Items.Single().Id.Should().Be(item.Id);
            persisted.Items.Single().UnitCostSnapshot.Should().Be(data.Product.UnitCost);
        }

        [Fact]
        public async Task GetAggregateTrackedByIdAndBusinessAsync_Should_Load_Tracked_Items()
        {
            await using var factory = CreateFactory();
            var ids = await SeedMovementWithItemAsync(factory);

            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);

            var movement = await repository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );

            movement.Should().NotBeNull();
            movement.Items.Should().ContainSingle();
            context.Entry(movement).State.Should().Be(EntityState.Unchanged);
            context.Entry(movement.Items.Single()).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetAggregateTrackedByIdAndBusinessAsync_Should_Isolate_Business()
        {
            await using var factory = CreateFactory();
            var ids = await SeedMovementWithItemAsync(factory);

            await using var scope = factory.Services.CreateAsyncScope();
            var repository = new FinancialMovementRepository(
                scope.ServiceProvider.GetRequiredService<MypeDbContext>()
            );

            var movement = await repository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                Guid.NewGuid(),
                CancellationToken.None
            );

            movement.Should().BeNull();
        }

        [Fact]
        public async Task SetOriginalVersions_Should_Set_Movement_And_Item_Original_Values()
        {
            await using var factory = CreateFactory();
            var ids = await SeedMovementWithItemAsync(factory);

            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);
            var movement = await repository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var item = movement.Items.Single();

            repository.SetOriginalVersions(movement, 17, item, 23);

            context
                .Entry(movement)
                .Property(current => current.Version)
                .OriginalValue.Should()
                .Be(17);
            context.Entry(item).Property(current => current.Version).OriginalValue.Should().Be(23);
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Translate_Stale_Movement_Version()
        {
            await using var factory = CreateFactory();
            var ids = await SeedMovementWithItemAsync(factory);
            await using var firstScope = factory.Services.CreateAsyncScope();
            await using var secondScope = factory.Services.CreateAsyncScope();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var secondContext = secondScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstRepository = new FinancialMovementRepository(firstContext);
            var secondRepository = new FinancialMovementRepository(secondContext);
            var firstMovement = await firstRepository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var secondMovement = await secondRepository.GetTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );

            firstMovement.UpdateDraftHeader(
                new DateOnly(2026, 10, 11),
                "Primera actualización",
                ids.UserId,
                UtcNow.AddHours(1)
            );
            await firstContext.SaveChangesAsync(CancellationToken.None);

            secondMovement.UpdateDraftHeader(
                new DateOnly(2026, 10, 12),
                "Actualización obsoleta",
                ids.UserId,
                UtcNow.AddHours(2)
            );
            var action = () => secondContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
            exception.Which.ErrorType.Should().Be(ApplicationErrorType.Conflict);
        }

        [Fact]
        public async Task GetDraft_And_ListDrafts_Should_Return_NoTracking_Projections()
        {
            await using var factory = CreateFactory();
            var ids = await SeedMovementWithItemAsync(factory);

            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);

            var detail = await repository.GetDraftByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var list = await repository.ListDraftsByBusinessAsync(
                ids.BusinessId,
                CancellationToken.None
            );

            detail.Should().NotBeNull();
            detail.Id.Should().Be(ids.MovementId);
            detail.TotalAmount.Should().Be(10m);
            list.Should().ContainSingle(current => current.Id == ids.MovementId);
            context.ChangeTracker.Entries<FinancialMovement>().Should().BeEmpty();
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static FinancialMovement CreateMovement(Guid businessId, Guid userId) =>
            FinancialMovement.CreateDraft(
                businessId,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 10),
                "Venta de integración",
                "PEN",
                userId,
                UtcNow
            );

        private static async Task<SeedData> SeedDependenciesAsync(MypeDbContext context)
        {
            var suffix = Guid.NewGuid().ToString("N");
            var user = User.Create(
                $"movement-{suffix}@example.com",
                $"MOVEMENT-{suffix}@EXAMPLE.COM",
                "hash",
                "Integration User",
                UtcNow
            );
            var currency = await context
                .Currencies.AsNoTracking()
                .SingleAsync(current => current.Code == "PEN");
            var business = Business.Create(
                $"Business {suffix}",
                null,
                null,
                currency.Id,
                user.Id,
                UtcNow
            );
            var category = Category.CreateDefault(
                business.Id,
                CategoryType.Sale,
                $"Category {suffix}",
                $"CATEGORY {suffix}".ToUpperInvariant(),
                user.Id,
                UtcNow
            );
            var product = Product.Create(
                business.Id,
                category.Id,
                $"Product {suffix}",
                $"PRODUCT {suffix}".ToUpperInvariant(),
                5m,
                2m,
                user.Id,
                UtcNow
            );

            context.Users.Add(user);
            context.Businesses.Add(business);
            context.Categories.Add(category);
            context.Products.Add(product);
            await context.SaveChangesAsync(CancellationToken.None);

            return new(user, business, product);
        }

        private static async Task<MovementIds> SeedMovementWithItemAsync(
            MypeWebApplicationFactory factory
        )
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);
            var data = await SeedDependenciesAsync(context);
            var movement = CreateMovement(data.Business.Id, data.User.Id);
            movement.AddSaleItem(data.Product, 2m, 5m, data.User.Id, UtcNow.AddMinutes(1));
            await repository.AddAsync(movement, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);

            return new(movement.Id, data.Business.Id, data.User.Id);
        }

        private sealed record SeedData(User User, Business Business, Product Product);

        private sealed record MovementIds(Guid MovementId, Guid BusinessId, Guid UserId);
    }
}
