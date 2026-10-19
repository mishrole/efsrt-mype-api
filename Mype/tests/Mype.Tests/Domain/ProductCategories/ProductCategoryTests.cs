using System;
using FluentAssertions;
using Mype.Domain.ProductCategories;

namespace Mype.Tests.Domain.ProductCategories
{
    public sealed class ProductCategoryTests
    {
        [Fact]
        public void Create_InitializesAnActiveCategory()
        {
            var businessId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var category = ProductCategory.Create(
                businessId,
                "Bebidas",
                "BEBIDAS",
                ProductCategoryPalette.Neutral,
                false,
                userId,
                now
            );

            category.BusinessId.Should().Be(businessId);
            category.Name.Should().Be("Bebidas");
            category.NormalizedName.Should().Be("BEBIDAS");
            category.Color.Should().Be("#64748B");
            category.IsActive.Should().BeTrue();
            category.IsDefault.Should().BeFalse();
        }

        [Fact]
        public void Update_ChangesNameAndColorWithoutChangingIdentity()
        {
            var userId = Guid.NewGuid();
            var category = ProductCategory.Create(
                Guid.NewGuid(),
                "Bebidas",
                "BEBIDAS",
                "#64748B",
                false,
                userId,
                DateTimeOffset.UtcNow
            );
            var id = category.Id;

            category.Update(
                "Donas",
                "DONAS",
                "#3F6FC4",
                userId,
                DateTimeOffset.UtcNow.AddMinutes(1)
            );

            category.Id.Should().Be(id);
            category.Name.Should().Be("Donas");
            category.NormalizedName.Should().Be("DONAS");
            category.Color.Should().Be("#3F6FC4");
        }

        [Fact]
        public void DeactivateAndReactivate_ReusesTheSameRecord()
        {
            var userId = Guid.NewGuid();
            var category = ProductCategory.Create(
                Guid.NewGuid(),
                "Otros",
                "OTROS",
                "#64748B",
                true,
                userId,
                DateTimeOffset.UtcNow
            );

            category.Deactivate(userId, DateTimeOffset.UtcNow.AddMinutes(1));
            category.IsActive.Should().BeFalse();
            category.DeactivatedAt.Should().NotBeNull();

            category.Reactivate(userId, DateTimeOffset.UtcNow.AddMinutes(2));
            category.IsActive.Should().BeTrue();
            category.DeactivatedAt.Should().BeNull();
            category.IsDefault.Should().BeTrue();
        }
    }
}
