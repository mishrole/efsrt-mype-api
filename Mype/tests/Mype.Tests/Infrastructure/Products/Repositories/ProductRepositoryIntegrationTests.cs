using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Domain.Categories;
using Mype.Domain.Products;
using Mype.Infrastructure.Persistence;
using Mype.Infrastructure.Products.Repositories;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.Products.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class ProductRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public ProductRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task Add_Exists_GetTracked_And_SetOriginalVersion_Should_Work()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var data = await SeedProductDependenciesAsync(context);
            var repository = new ProductRepository(context);
            var product = CreateProduct(data, "Gaseosa");

            await repository.AddAsync(product, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            (
                await repository.ExistsByBusinessAndNormalizedNameAsync(
                    data.Seed.Business.Id,
                    product.NormalizedName,
                    CancellationToken.None
                )
            )
                .Should()
                .BeTrue();
            var tracked = await repository.GetTrackedByIdAndBusinessAsync(
                product.Id,
                data.Seed.Business.Id,
                CancellationToken.None
            );
            tracked.Should().NotBeNull();
            context.Entry(tracked).State.Should().Be(EntityState.Unchanged);

            repository.SetOriginalVersion(tracked, 41);
            context.Entry(tracked).Property(item => item.Version).OriginalValue.Should().Be(41);
        }

        [Fact]
        public async Task ExistsOther_Should_Exclude_Selected_Product()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var data = await SeedProductDependenciesAsync(context);
            var repository = new ProductRepository(context);
            var first = CreateProduct(data, "Producto A");
            var second = CreateProduct(data, "Producto B");
            await repository.AddAsync(first, CancellationToken.None);
            await repository.AddAsync(second, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);

            (
                await repository.ExistsOtherByBusinessAndNormalizedNameAsync(
                    data.Seed.Business.Id,
                    first.NormalizedName,
                    first.Id,
                    CancellationToken.None
                )
            )
                .Should()
                .BeFalse();
            (
                await repository.ExistsOtherByBusinessAndNormalizedNameAsync(
                    data.Seed.Business.Id,
                    second.NormalizedName,
                    first.Id,
                    CancellationToken.None
                )
            )
                .Should()
                .BeTrue();
        }

        [Fact]
        public async Task ListByBusinessAsync_Should_Filter_Search_Category_And_Status()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var data = await SeedProductDependenciesAsync(context);
            var repository = new ProductRepository(context);
            var gaseosa = CreateProduct(data, "Gaseosa familiar");
            var agua = CreateProduct(data, "Agua mineral");
            var inactive = CreateProduct(data, "Gaseosa inactiva");
            inactive.Deactivate(data.Seed.User.Id, RepositoryTestData.UtcNow.AddMinutes(1));
            await repository.AddAsync(gaseosa, CancellationToken.None);
            await repository.AddAsync(agua, CancellationToken.None);
            await repository.AddAsync(inactive, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var result = await repository.ListByBusinessAsync(
                data.Seed.Business.Id,
                "GASEOSA",
                data.SaleCategory.Id,
                true,
                false,
                CancellationToken.None
            );

            result.Should().ContainSingle(item => item.Id == gaseosa.Id);
            result.Should().NotContain(item => item.Id == agua.Id || item.Id == inactive.Id);
            context.ChangeTracker.Entries<Product>().Should().BeEmpty();
        }

        [Fact]
        public async Task ListByBusinessAsync_AvailableForSale_Should_Require_Active_Product_And_Category()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var data = await SeedProductDependenciesAsync(context);
            var repository = new ProductRepository(context);
            var available = CreateProduct(data, "Disponible");
            var inactiveProduct = CreateProduct(data, "Producto inactivo");
            inactiveProduct.Deactivate(data.Seed.User.Id, RepositoryTestData.UtcNow.AddMinutes(1));
            var inactiveCategory = Category.CreateDefault(
                data.Seed.Business.Id,
                CategoryType.Sale,
                "Categoría inactiva",
                $"CATEGORY {Guid.NewGuid():N}".ToUpperInvariant(),
                data.Seed.User.Id,
                RepositoryTestData.UtcNow
            );
            inactiveCategory.Deactivate(data.Seed.User.Id, RepositoryTestData.UtcNow.AddMinutes(1));
            context.Categories.Add(inactiveCategory);
            var unavailableByCategory = Product.Create(
                data.Seed.Business.Id,
                inactiveCategory.Id,
                "Categoría no disponible",
                $"PRODUCT {Guid.NewGuid():N}".ToUpperInvariant(),
                4m,
                2m,
                data.Seed.User.Id,
                RepositoryTestData.UtcNow
            );
            await repository.AddAsync(available, CancellationToken.None);
            await repository.AddAsync(inactiveProduct, CancellationToken.None);
            await repository.AddAsync(unavailableByCategory, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);

            var result = await repository.ListByBusinessAsync(
                data.Seed.Business.Id,
                null,
                null,
                null,
                true,
                CancellationToken.None
            );

            result.Should().ContainSingle(item => item.Id == available.Id);
        }

        [Fact]
        public async Task GetByIdAndBusinessAsync_Should_Project_Category_And_NotTrack_Product()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var data = await SeedProductDependenciesAsync(context);
            var repository = new ProductRepository(context);
            var product = CreateProduct(data, "Detalle");
            await repository.AddAsync(product, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var result = await repository.GetByIdAndBusinessAsync(
                product.Id,
                data.Seed.Business.Id,
                CancellationToken.None
            );

            result.Should().NotBeNull();
            result.CategoryId.Should().Be(data.SaleCategory.Id);
            result.CategoryName.Should().Be(data.SaleCategory.Name);
            result.CategoryType.Should().Be(CategoryType.Sale);
            context.ChangeTracker.Entries<Product>().Should().BeEmpty();
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static async Task<ProductSeed> SeedProductDependenciesAsync(MypeDbContext context)
        {
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var category = Category.CreateDefault(
                seed.Business.Id,
                CategoryType.Sale,
                "Productos",
                $"PRODUCTS {seed.Suffix}".ToUpperInvariant(),
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
            context.Categories.Add(category);
            await context.SaveChangesAsync(CancellationToken.None);
            return new(seed, category);
        }

        private static Product CreateProduct(ProductSeed data, string name) =>
            Product.Create(
                data.Seed.Business.Id,
                data.SaleCategory.Id,
                name,
                $"{name} {Guid.NewGuid():N}".ToUpperInvariant(),
                5m,
                2m,
                data.Seed.User.Id,
                RepositoryTestData.UtcNow
            );

        private sealed record ProductSeed(BusinessSeed Seed, Category SaleCategory);
    }
}
