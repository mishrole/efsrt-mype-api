using System;
using FluentAssertions;
using Mype.Application.FinancialMovements.Models;

namespace Mype.Tests.Application.FinancialMovements.Models
{
    public class FinancialMovementItemResultTests
    {
        [Fact]
        public void FinancialMovementItemResult_Should_Expose_All_Values()
        {
            var itemId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var retiredAt = DateTimeOffset.UtcNow;

            var result = new FinancialMovementItemResult(
                itemId,
                productId,
                "Gaseosa",
                categoryId,
                "Productos",
                3.5000m,
                4.20m,
                2.10m,
                14.70m,
                7.35m,
                7.35m,
                false,
                retiredAt,
                8
            );

            result.Id.Should().Be(itemId);
            result.ProductId.Should().Be(productId);
            result.ProductName.Should().Be("Gaseosa");
            result.CategoryId.Should().Be(categoryId);
            result.CategoryName.Should().Be("Productos");
            result.Quantity.Should().Be(3.5000m);
            result.UnitAmount.Should().Be(4.20m);
            result.UnitCostSnapshot.Should().Be(2.10m);
            result.SubtotalAmount.Should().Be(14.70m);
            result.EstimatedCost.Should().Be(7.35m);
            result.EstimatedMargin.Should().Be(7.35m);
            result.IsActive.Should().BeFalse();
            result.RetiredAt.Should().Be(retiredAt);
            result.Version.Should().Be(8);
        }

        [Fact]
        public void FinancialMovementItemMaintenanceResult_Should_Expose_All_Values()
        {
            var item = new FinancialMovementItemResult(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Gaseosa",
                Guid.NewGuid(),
                "Productos",
                2m,
                5m,
                3m,
                10m,
                6m,
                4m,
                true,
                null,
                4
            );

            var result = new FinancialMovementItemMaintenanceResult(item, 25m, 9);

            result.Item.Should().BeSameAs(item);
            result.MovementTotal.Should().Be(25m);
            result.MovementVersion.Should().Be(9);
        }

        [Fact]
        public void RetiredFinancialMovementItemResult_Should_Expose_All_Values()
        {
            var itemId = Guid.NewGuid();
            var retiredAt = DateTimeOffset.UtcNow;

            var result = new RetiredFinancialMovementItemResult(
                itemId,
                false,
                retiredAt,
                6,
                15m,
                10
            );

            result.ItemId.Should().Be(itemId);
            result.IsActive.Should().BeFalse();
            result.RetiredAt.Should().Be(retiredAt);
            result.ItemVersion.Should().Be(6);
            result.MovementTotal.Should().Be(15m);
            result.MovementVersion.Should().Be(10);
        }
    }
}
