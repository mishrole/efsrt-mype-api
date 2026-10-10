using System;
using FluentAssertions;
using Mype.Domain.Products;

namespace Mype.Tests.Domain.Products
{
    public class ProductTests
    {
        private static readonly DateTimeOffset CreatedAt = new(
            2026,
            10,
            8,
            12,
            0,
            0,
            TimeSpan.Zero
        );

        [Fact]
        public void Create_Should_Initialize_Product()
        {
            var p = Create();
            p.Id.Should().NotBeEmpty();
            p.IsActive.Should().BeTrue();
            p.CreatedAt.Should().Be(CreatedAt);
        }

        [Fact]
        public void Update_Should_Change_Editable_Fields_And_Audit()
        {
            var p = Create();
            var category = Guid.NewGuid();
            var user = Guid.NewGuid();
            var at = CreatedAt.AddHours(1);
            p.Update(category, "Agua", "AGUA", 2m, 1m, user, at);
            p.CategoryId.Should().Be(category);
            p.Name.Should().Be("Agua");
            p.NormalizedName.Should().Be("AGUA");
            p.SalePrice.Should().Be(2m);
            p.UnitCost.Should().Be(1m);
            p.UpdatedByUserId.Should().Be(user);
            p.UpdatedAt.Should().Be(at);
            p.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Deactivate_And_Reactivate_Should_Update_State()
        {
            var p = Create();
            var user = Guid.NewGuid();
            var at = CreatedAt.AddHours(1);
            p.Deactivate(user, at);
            p.IsActive.Should().BeFalse();
            p.DeactivatedAt.Should().Be(at);
            p.Reactivate(user, at.AddHours(1));
            p.IsActive.Should().BeTrue();
            p.DeactivatedAt.Should().BeNull();
        }

        private static Product Create() =>
            Product.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Gaseosa",
                "GASEOSA",
                3.5m,
                2.2m,
                Guid.NewGuid(),
                CreatedAt
            );
    }
}
