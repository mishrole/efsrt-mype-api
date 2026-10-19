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
using Mype.Domain.ProductCategories;
using Mype.Domain.Products;
using Mype.Infrastructure.FinancialMovements.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.FinancialMovements.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class FinancialMovementConfirmationConcurrencyIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public FinancialMovementConfirmationConcurrencyIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task SetOriginalVersions_Should_Set_All_Active_Item_Versions()
        {
            await using var factory = CreateFactory();
            var ids = await SeedSaleAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);
            var movement = await repository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var versions = movement.Items.ToDictionary(item => item.Id, _ => 23u);

            repository.SetOriginalVersions(movement, 17, versions);

            context.Entry(movement).Property(item => item.Version).OriginalValue.Should().Be(17);
            foreach (var item in movement.Items)
            {
                context
                    .Entry(item)
                    .Property(current => current.Version)
                    .OriginalValue.Should()
                    .Be(23);
                context
                    .Entry(item)
                    .Property(current => current.SubtotalAmount)
                    .IsModified.Should()
                    .BeTrue();
            }
        }

        [Fact]
        public async Task SaveChangesAsync_Should_Roll_Back_When_Any_Active_Item_Version_Is_Stale()
        {
            await using var factory = CreateFactory();
            var ids = await SeedSaleAsync(factory);
            await using var firstScope = factory.Services.CreateAsyncScope();
            await using var secondScope = factory.Services.CreateAsyncScope();
            var firstContext = firstScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var secondContext = secondScope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstRepository = new FinancialMovementRepository(firstContext);
            var secondRepository = new FinancialMovementRepository(secondContext);
            var first = await firstRepository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var second = await secondRepository.GetAggregateTrackedByIdAndBusinessAsync(
                ids.MovementId,
                ids.BusinessId,
                CancellationToken.None
            );
            var firstItem = first.Items.Single();
            var secondItem = second.Items.Single();

            first.UpdateSaleItem(
                firstItem.Id,
                ids.Product,
                3m,
                5m,
                ids.UserId,
                RepositoryTestData.UtcNow.AddHours(1)
            );
            await firstContext.SaveChangesAsync(CancellationToken.None);

            secondRepository.SetOriginalVersions(
                second,
                second.Version,
                new Dictionary<Guid, uint> { [secondItem.Id] = secondItem.Version }
            );
            second.Confirm(ids.UserId, RepositoryTestData.UtcNow.AddHours(2));
            var action = () => secondContext.SaveChangesAsync(CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ApplicationErrorException>();
            exception.Which.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
            secondContext.ChangeTracker.Clear();
            var persisted = await secondContext
                .FinancialMovements.AsNoTracking()
                .SingleAsync(item => item.Id == ids.MovementId);
            persisted.Status.Should().Be(FinancialMovementStatus.Draft);
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
                $"PRODUCTOS {seed.Suffix}",
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            var productCategory = ProductCategory.Create(
                seed.Business.Id,
                "Otros",
                $"OTROS {seed.Suffix}".ToUpperInvariant(),
                ProductCategoryPalette.Neutral,
                true,
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            var product = Product.Create(
                seed.Business.Id,
                category.Id,
                productCategory.Id,
                "Gaseosa",
                $"GASEOSA {seed.Suffix}",
                4m,
                2m,
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            context.Categories.Add(category);
            context.ProductCategories.Add(productCategory);
            context.Products.Add(product);
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
            context.FinancialMovements.Add(movement);
            await context.SaveChangesAsync(CancellationToken.None);
            return new(movement.Id, seed.Business.Id, seed.User.Id, product);
        }

        private sealed record SeedIds(
            Guid MovementId,
            Guid BusinessId,
            Guid UserId,
            Product Product
        );
    }
}
