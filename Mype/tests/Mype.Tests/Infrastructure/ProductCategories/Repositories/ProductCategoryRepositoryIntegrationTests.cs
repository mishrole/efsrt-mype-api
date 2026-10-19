using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Domain.ProductCategories;
using Mype.Infrastructure.Persistence;
using Mype.Infrastructure.ProductCategories.Repositories;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.ProductCategories.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class ProductCategoryRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public ProductCategoryRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task Add_Read_Track_List_And_SetOriginalVersion_Should_Work()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var businessSeed = await RepositoryTestData.SeedBusinessAsync(dbContext);
            var repository = new ProductCategoryRepository(dbContext);
            var productCategory = ProductCategory.Create(
                businessSeed.Business.Id,
                "Bebidas frías",
                $"BEBIDAS FRIAS {businessSeed.Suffix}".ToUpperInvariant(),
                "#3F6FC4",
                false,
                businessSeed.User.Id,
                RepositoryTestData.UtcNow
            );
            await repository.AddAsync(productCategory, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            var readOnlyProductCategory = await repository.GetByIdAndBusinessAsync(
                productCategory.Id,
                businessSeed.Business.Id,
                CancellationToken.None
            );
            readOnlyProductCategory.Should().NotBeNull();
            dbContext.ChangeTracker.Entries<ProductCategory>().Should().BeEmpty();
            var trackedProductCategory = await repository.GetTrackedByIdAndBusinessAsync(
                productCategory.Id,
                businessSeed.Business.Id,
                CancellationToken.None
            );
            dbContext.Entry(trackedProductCategory).State.Should().Be(EntityState.Unchanged);
            repository.SetOriginalVersion(trackedProductCategory, 17);
            dbContext
                .Entry(trackedProductCategory)
                .Property(currentProductCategory => currentProductCategory.Version)
                .OriginalValue.Should()
                .Be(17);
            dbContext.ChangeTracker.Clear();
            var searchResult = await repository.ListAsync(
                businessSeed.Business.Id,
                "BEBIDAS",
                true,
                CancellationToken.None
            );
            searchResult
                .Should()
                .ContainSingle(currentProductCategory =>
                    currentProductCategory.Id == productCategory.Id
                );
            dbContext.ChangeTracker.Entries<ProductCategory>().Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAndBusinessAsync_Should_Isolate_Business()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var businessSeed = await RepositoryTestData.SeedBusinessAsync(dbContext);
            var repository = new ProductCategoryRepository(dbContext);
            var productCategory = ProductCategory.Create(
                businessSeed.Business.Id,
                "Otros",
                $"OTROS {businessSeed.Suffix}".ToUpperInvariant(),
                ProductCategoryPalette.Neutral,
                true,
                businessSeed.User.Id,
                RepositoryTestData.UtcNow
            );
            await repository.AddAsync(productCategory, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            var foreignResult = await repository.GetByIdAndBusinessAsync(
                productCategory.Id,
                Guid.NewGuid(),
                CancellationToken.None
            );
            foreignResult.Should().BeNull();
        }

        [Fact]
        public async Task ListAsync_Should_Cover_All_Optional_Filter_Branches()
        {
            await using var factory = new MypeWebApplicationFactory(_database.ConnectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var businessSeed = await RepositoryTestData.SeedBusinessAsync(dbContext);
            var repository = new ProductCategoryRepository(dbContext);
            var activeProductCategory = ProductCategory.Create(
                businessSeed.Business.Id,
                "Bebidas",
                $"BEBIDAS {businessSeed.Suffix}".ToUpperInvariant(),
                "#3F6FC4",
                false,
                businessSeed.User.Id,
                RepositoryTestData.UtcNow
            );
            var inactiveProductCategory = ProductCategory.Create(
                businessSeed.Business.Id,
                "Snacks",
                $"SNACKS {businessSeed.Suffix}".ToUpperInvariant(),
                "#F59E0B",
                false,
                businessSeed.User.Id,
                RepositoryTestData.UtcNow
            );
            inactiveProductCategory.Deactivate(
                businessSeed.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(1)
            );
            await repository.AddAsync(activeProductCategory, CancellationToken.None);
            await repository.AddAsync(inactiveProductCategory, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();

            var allProductCategories = await repository.ListAsync(
                businessSeed.Business.Id,
                null,
                null,
                CancellationToken.None
            );
            var inactiveProductCategories = await repository.ListAsync(
                businessSeed.Business.Id,
                null,
                false,
                CancellationToken.None
            );
            var categoryByName = await repository.GetByNormalizedNameAsync(
                businessSeed.Business.Id,
                activeProductCategory.NormalizedName,
                CancellationToken.None
            );
            var missingTrackedProductCategory = await repository.GetTrackedByIdAndBusinessAsync(
                Guid.NewGuid(),
                businessSeed.Business.Id,
                CancellationToken.None
            );

            allProductCategories
                .Should()
                .Contain(currentProductCategory =>
                    currentProductCategory.Id == activeProductCategory.Id
                );
            allProductCategories
                .Should()
                .Contain(currentProductCategory =>
                    currentProductCategory.Id == inactiveProductCategory.Id
                );
            inactiveProductCategories
                .Should()
                .ContainSingle(currentProductCategory =>
                    currentProductCategory.Id == inactiveProductCategory.Id
                );
            categoryByName.Should().NotBeNull();
            missingTrackedProductCategory.Should().BeNull();
            dbContext.ChangeTracker.Entries<ProductCategory>().Should().BeEmpty();
        }
    }
}
