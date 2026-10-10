using FluentAssertions;
using Mype.Domain.Products;
using System;

namespace Mype.Tests.Domain.Products
{
    public class ProductTests
    {
        [Fact]
        public void Create_Should_Initialize_Product()
        {
            var businessId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var utcNow = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

            var product = Product.Create(
                businessId,
                categoryId,
                "Gaseosa 500 ml",
                "GASEOSA 500 ML",
                3.50m,
                2.20m,
                userId,
                utcNow
            );

            product.Id.Should().NotBeEmpty();
            product.BusinessId.Should().Be(businessId);
            product.CategoryId.Should().Be(categoryId);
            product.Name.Should().Be("Gaseosa 500 ml");
            product.NormalizedName.Should().Be("GASEOSA 500 ML");
            product.SalePrice.Should().Be(3.50m);
            product.UnitCost.Should().Be(2.20m);
            product.IsActive.Should().BeTrue();
            product.IsAvailable().Should().BeTrue();
            product.CreatedByUserId.Should().Be(userId);
            product.UpdatedByUserId.Should().Be(userId);
            product.CreatedAt.Should().Be(utcNow);
            product.UpdatedAt.Should().Be(utcNow);
            product.DeactivatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_Should_Allow_Zero_Amounts()
        {
            var product = Product.Create(
                Guid.NewGuid(), Guid.NewGuid(), "Muestra", "MUESTRA",
                0m, 0m, Guid.NewGuid(), DateTimeOffset.UtcNow
            );

            product.SalePrice.Should().Be(0m);
            product.UnitCost.Should().Be(0m);
        }
    }
}
