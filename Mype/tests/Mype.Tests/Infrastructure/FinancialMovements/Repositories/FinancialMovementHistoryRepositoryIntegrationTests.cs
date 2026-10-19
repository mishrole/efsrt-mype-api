using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Domain.MovementEvidences;
using Mype.Domain.ProductCategories;
using Mype.Domain.Products;
using Mype.Infrastructure.FinancialMovements.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Tests.Infrastructure.Common;
using Mype.Tests.Integration;

namespace Mype.Tests.Infrastructure.FinancialMovements.Repositories
{
    [Collection(PostgreSqlCollection.Name)]
    public sealed class FinancialMovementHistoryRepositoryIntegrationTests
    {
        private readonly PostgreSqlFixture _database;

        public FinancialMovementHistoryRepositoryIntegrationTests(PostgreSqlFixture database)
        {
            _database = database;
        }

        [Fact]
        public async Task SearchHistoryByBusinessAsync_Should_Isolate_Order_Paginate_And_Project_Summaries()
        {
            await using var factory = CreateFactory();
            var seed = await SeedHistoryAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);

            var firstPage = await repository.SearchHistoryByBusinessAsync(
                seed.BusinessId,
                new(null, null, null, null, null, 1, 2),
                CancellationToken.None
            );
            var secondPage = await repository.SearchHistoryByBusinessAsync(
                seed.BusinessId,
                new(null, null, null, null, null, 2, 2),
                CancellationToken.None
            );

            firstPage.TotalCount.Should().Be(3);
            firstPage
                .Items.Select(item => item.Id)
                .Should()
                .Equal(seed.LatestMovementId, seed.MiddleMovementId);
            secondPage.Items.Should().ContainSingle(item => item.Id == seed.OldestMovementId);
            firstPage.Items.Should().OnlyContain(item => item.Id != seed.OtherBusinessMovementId);
            firstPage
                .Items.Single(item => item.Id == seed.LatestMovementId)
                .ActiveItemCount.Should()
                .Be(1);
            firstPage
                .Items.Single(item => item.Id == seed.LatestMovementId)
                .HasActiveEvidence.Should()
                .BeTrue();
            context.ChangeTracker.Entries().Should().BeEmpty();
        }

        [Fact]
        public async Task SearchHistoryByBusinessAsync_Should_Apply_All_Filters_With_And_Logic()
        {
            await using var factory = CreateFactory();
            var seed = await SeedHistoryAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);

            var page = await repository.SearchHistoryByBusinessAsync(
                seed.BusinessId,
                new(
                    FinancialMovementType.Sale,
                    FinancialMovementStatus.Confirmed,
                    new DateOnly(2026, 10, 11),
                    new DateOnly(2026, 10, 11),
                    seed.SaleCategoryId,
                    1,
                    20
                ),
                CancellationToken.None
            );

            page.TotalCount.Should().Be(1);
            page.Items.Should()
                .ContainSingle(item =>
                    item.Id == seed.LatestMovementId
                    && item.Type == FinancialMovementType.Sale
                    && item.Status == FinancialMovementStatus.Confirmed
                    && item.MovementDate == new DateOnly(2026, 10, 11)
                );
        }

        [Fact]
        public async Task SearchHistoryByBusinessAsync_Should_Apply_Open_Date_Ranges_And_Ignore_Retired_Category_Items()
        {
            await using var factory = CreateFactory();
            var seed = await SeedHistoryAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var repository = new FinancialMovementRepository(
                scope.ServiceProvider.GetRequiredService<MypeDbContext>()
            );

            var fromOnly = await repository.SearchHistoryByBusinessAsync(
                seed.BusinessId,
                new(null, null, new DateOnly(2026, 10, 10), null, null, 1, 20),
                CancellationToken.None
            );
            var toOnly = await repository.SearchHistoryByBusinessAsync(
                seed.BusinessId,
                new(null, null, null, new DateOnly(2026, 10, 9), null, 1, 20),
                CancellationToken.None
            );
            var retiredCategory = await repository.SearchHistoryByBusinessAsync(
                seed.BusinessId,
                new(null, null, null, null, seed.RetiredOnlyCategoryId, 1, 20),
                CancellationToken.None
            );

            fromOnly
                .Items.Select(item => item.Id)
                .Should()
                .BeEquivalentTo([seed.LatestMovementId, seed.MiddleMovementId]);
            toOnly.Items.Should().ContainSingle(item => item.Id == seed.OldestMovementId);
            retiredCategory.TotalCount.Should().Be(0);
            retiredCategory.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetHistoryDetailByIdAndBusinessAsync_Should_Project_Items_Catalogs_And_Evidences_Without_Tracking()
        {
            await using var factory = CreateFactory();
            var seed = await SeedHistoryAsync(factory);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var repository = new FinancialMovementRepository(context);

            var detail = await repository.GetHistoryDetailByIdAndBusinessAsync(
                seed.LatestMovementId,
                seed.BusinessId,
                CancellationToken.None
            );
            var foreign = await repository.GetHistoryDetailByIdAndBusinessAsync(
                seed.OtherBusinessMovementId,
                seed.BusinessId,
                CancellationToken.None
            );

            detail.Should().NotBeNull();
            detail.Status.Should().Be(FinancialMovementStatus.Confirmed);
            detail.ConfirmedAt.Should().NotBeNull();
            detail.Items.Should().HaveCount(2);
            detail
                .Items.Should()
                .ContainSingle(item =>
                    item.IsActive
                    && item.CategoryId == seed.SaleCategoryId
                    && item.CategoryName == "Ventas"
                    && item.CategoryIsActive == false
                    && item.ProductId == seed.ProductId
                    && item.ProductName == "Producto histórico"
                    && item.ProductIsActive == false
                );
            detail
                .Items.Should()
                .ContainSingle(item =>
                    !item.IsActive && item.CategoryId == seed.RetiredOnlyCategoryId
                );
            detail.Evidences.Should().HaveCount(2);
            detail
                .Evidences.Should()
                .ContainSingle(evidence => evidence.Status == MovementEvidenceStatus.Active);
            detail
                .Evidences.Should()
                .ContainSingle(evidence => evidence.Status == MovementEvidenceStatus.PendingPurge);
            foreign.Should().BeNull();
            context.ChangeTracker.Entries().Should().BeEmpty();
        }

        private MypeWebApplicationFactory CreateFactory() => new(_database.ConnectionString);

        private static async Task<HistorySeed> SeedHistoryAsync(MypeWebApplicationFactory factory)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<MypeDbContext>();
            var firstBusiness = await RepositoryTestData.SeedBusinessAsync(context);
            var secondBusiness = await RepositoryTestData.SeedBusinessAsync(context);
            var saleCategory = Category.CreateDefault(
                firstBusiness.Business.Id,
                CategoryType.Sale,
                "Ventas",
                $"VENTAS {firstBusiness.Suffix}".ToUpperInvariant(),
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow
            );
            var retiredOnlyCategory = Category.CreateDefault(
                firstBusiness.Business.Id,
                CategoryType.Sale,
                "Retirada",
                $"RETIRADA {firstBusiness.Suffix}".ToUpperInvariant(),
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow
            );
            var expenseCategory = Category.CreateDefault(
                firstBusiness.Business.Id,
                CategoryType.Expense,
                "Gastos",
                $"GASTOS {firstBusiness.Suffix}".ToUpperInvariant(),
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow
            );
            var otherCategory = Category.CreateDefault(
                secondBusiness.Business.Id,
                CategoryType.Expense,
                "Otros",
                $"OTROS {secondBusiness.Suffix}".ToUpperInvariant(),
                secondBusiness.User.Id,
                RepositoryTestData.UtcNow
            );
            var productCategory = ProductCategory.Create(
                firstBusiness.Business.Id,
                "Otros",
                $"OTROS {firstBusiness.Suffix}".ToUpperInvariant(),
                ProductCategoryPalette.Neutral,
                true,
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow
            );
            var product = Product.Create(
                firstBusiness.Business.Id,
                saleCategory.Id,
                productCategory.Id,
                "Producto histórico",
                $"PRODUCTO {firstBusiness.Suffix}".ToUpperInvariant(),
                5m,
                2m,
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow
            );
            context.Categories.AddRange(
                saleCategory,
                retiredOnlyCategory,
                expenseCategory,
                otherCategory
            );
            var retiredProduct = Product.Create(
                firstBusiness.Business.Id,
                retiredOnlyCategory.Id,
                productCategory.Id,
                "Producto retirado",
                $"PRODUCTO RETIRADO {firstBusiness.Suffix}".ToUpperInvariant(),
                5m,
                2m,
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow
            );
            context.ProductCategories.Add(productCategory);
            context.Products.AddRange(product, retiredProduct);

            var latest = FinancialMovement.CreateDraft(
                firstBusiness.Business.Id,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 11),
                "Venta confirmada",
                "PEN",
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(3)
            );
            latest.AddSaleItem(
                product,
                2m,
                5m,
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(4)
            );
            var retired = latest.AddSaleItem(
                retiredProduct,
                1m,
                5m,
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(5)
            );
            latest.RetireItem(
                retired.Id,
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(6)
            );
            latest.Confirm(firstBusiness.User.Id, RepositoryTestData.UtcNow.AddMinutes(7));

            var middle = FinancialMovement.CreateDraft(
                firstBusiness.Business.Id,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 10),
                "Gasto descartado",
                "PEN",
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(2)
            );
            middle.AddExpenseItem(
                expenseCategory.Id,
                "Servicio",
                1m,
                8m,
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(3)
            );
            middle.Discard(firstBusiness.User.Id, RepositoryTestData.UtcNow.AddMinutes(8));

            var oldest = FinancialMovement.CreateDraft(
                firstBusiness.Business.Id,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 9),
                "Gasto borrador",
                "PEN",
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(1)
            );
            var other = FinancialMovement.CreateDraft(
                secondBusiness.Business.Id,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 12),
                "Movimiento ajeno",
                "PEN",
                secondBusiness.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(9)
            );
            other.AddExpenseItem(
                otherCategory.Id,
                "Ajeno",
                1m,
                1m,
                secondBusiness.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(10)
            );
            context.FinancialMovements.AddRange(latest, middle, oldest, other);
            await context.SaveChangesAsync(CancellationToken.None);

            var activeEvidence = CreateEvidence(
                firstBusiness.Business.Id,
                latest.Id,
                firstBusiness.User.Id,
                "active.pdf",
                RepositoryTestData.UtcNow.AddMinutes(10)
            );
            var pendingEvidence = CreateEvidence(
                firstBusiness.Business.Id,
                latest.Id,
                firstBusiness.User.Id,
                "pending.pdf",
                RepositoryTestData.UtcNow.AddMinutes(11)
            );
            pendingEvidence.MarkPendingPurge(
                EvidenceRemovalReason.Removed,
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(12),
                RepositoryTestData.UtcNow.AddDays(30)
            );
            context.MovementEvidences.AddRange(activeEvidence, pendingEvidence);
            saleCategory.Deactivate(
                firstBusiness.User.Id,
                RepositoryTestData.UtcNow.AddMinutes(13)
            );
            product.Deactivate(firstBusiness.User.Id, RepositoryTestData.UtcNow.AddMinutes(13));
            await context.SaveChangesAsync(CancellationToken.None);

            return new(
                firstBusiness.Business.Id,
                latest.Id,
                middle.Id,
                oldest.Id,
                other.Id,
                saleCategory.Id,
                retiredOnlyCategory.Id,
                product.Id
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
                $"history/{movementId:N}/{id:N}/{fileName}",
                fileName,
                "application/pdf",
                128,
                userId,
                uploadedAt
            );
        }

        private sealed record HistorySeed(
            Guid BusinessId,
            Guid LatestMovementId,
            Guid MiddleMovementId,
            Guid OldestMovementId,
            Guid OtherBusinessMovementId,
            Guid SaleCategoryId,
            Guid RetiredOnlyCategoryId,
            Guid ProductId
        );
    }
}
