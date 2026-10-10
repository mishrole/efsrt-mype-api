using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mype.Domain.Categories;
using Mype.Infrastructure.Categories.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.Categories.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class CategoryRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public CategoryRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task AddAsync_Get_And_Exists_Should_Persist_And_Isolate_Business()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var repository = new CategoryRepository(context);
            var category = CreateCategory(seed, CategoryType.Sale, "Productos");

            await repository.AddAsync(category, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            (
                await repository.ExistsByIdAndBusinessAsync(
                    category.Id,
                    seed.Business.Id,
                    CancellationToken.None
                )
            )
                .Should()
                .BeTrue();
            (
                await repository.ExistsByIdAndBusinessAsync(
                    category.Id,
                    Guid.NewGuid(),
                    CancellationToken.None
                )
            )
                .Should()
                .BeFalse();

            var result = await repository.GetByIdAndBusinessAsync(
                category.Id,
                seed.Business.Id,
                CancellationToken.None
            );
            result.Should().NotBeNull();
            result.Name.Should().Be("Productos");
            context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task ListByBusinessAsync_Should_Filter_By_Type_And_Status()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var repository = new CategoryRepository(context);
            var activeSale = CreateCategory(seed, CategoryType.Sale, "Venta activa");
            var inactiveSale = CreateCategory(seed, CategoryType.Sale, "Venta inactiva");
            var activeExpense = CreateCategory(seed, CategoryType.Expense, "Gasto activo");
            inactiveSale.Deactivate(seed.User.Id, RepositoryTestData.UtcNow.AddMinutes(1));
            await repository.AddAsync(activeSale, CancellationToken.None);
            await repository.AddAsync(inactiveSale, CancellationToken.None);
            await repository.AddAsync(activeExpense, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
            context.ChangeTracker.Clear();

            var saleCategories = await repository.ListByBusinessAsync(
                seed.Business.Id,
                CategoryType.Sale,
                true,
                CancellationToken.None
            );
            var inactiveCategories = await repository.ListByBusinessAsync(
                seed.Business.Id,
                null,
                false,
                CancellationToken.None
            );

            saleCategories.Should().ContainSingle(item => item.Id == activeSale.Id);
            saleCategories.Should().NotContain(item => item.Id == inactiveSale.Id);
            saleCategories.Should().NotContain(item => item.Id == activeExpense.Id);
            inactiveCategories.Should().ContainSingle(item => item.Id == inactiveSale.Id);
            context.ChangeTracker.Entries<Category>().Should().BeEmpty();
        }

        [Fact]
        public async Task ListByBusinessAsync_Should_Order_By_Type_And_Name()
        {
            await using var factory = CreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var seed = await RepositoryTestData.SeedBusinessAsync(context);
            var repository = new CategoryRepository(context);
            await repository.AddAsync(
                CreateCategory(seed, CategoryType.Expense, "Z gasto"),
                CancellationToken.None
            );
            await repository.AddAsync(
                CreateCategory(seed, CategoryType.Sale, "B venta"),
                CancellationToken.None
            );
            await repository.AddAsync(
                CreateCategory(seed, CategoryType.Sale, "A venta"),
                CancellationToken.None
            );
            await context.SaveChangesAsync(CancellationToken.None);

            var result = await repository.ListByBusinessAsync(
                seed.Business.Id,
                null,
                null,
                CancellationToken.None
            );

            result
                .Select(item => item.Name)
                .Should()
                .ContainInOrder("A venta", "B venta", "Z gasto");
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static Category CreateCategory(BusinessSeed seed, CategoryType type, string name) =>
            Category.CreateDefault(
                seed.Business.Id,
                type,
                name,
                $"{name} {Guid.NewGuid():N}".ToUpperInvariant(),
                seed.User.Id,
                RepositoryTestData.UtcNow
            );
    }
}
