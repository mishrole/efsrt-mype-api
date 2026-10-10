using System;
using FluentAssertions;
using Mype.Application.FinancialMovements.Models;

namespace Mype.Tests.Application.FinancialMovements.Models
{
    public sealed class ExpenseMovementItemResultTests
    {
        [Fact]
        public void Results_Should_Expose_All_Values()
        {
            var id = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var item = new ExpenseMovementItemResult(
                id,
                "Bolsas",
                categoryId,
                "Insumos",
                2m,
                15.50m,
                31m,
                true,
                null,
                7
            );
            var result = new ExpenseMovementItemMaintenanceResult(item, 45m, 8);

            item.Id.Should().Be(id);
            item.Description.Should().Be("Bolsas");
            item.CategoryId.Should().Be(categoryId);
            item.CategoryName.Should().Be("Insumos");
            item.Quantity.Should().Be(2m);
            item.UnitAmount.Should().Be(15.50m);
            item.SubtotalAmount.Should().Be(31m);
            item.IsActive.Should().BeTrue();
            item.RetiredAt.Should().BeNull();
            item.Version.Should().Be(7);
            result.Item.Should().BeSameAs(item);
            result.MovementTotal.Should().Be(45m);
            result.MovementVersion.Should().Be(8);
        }
    }
}
