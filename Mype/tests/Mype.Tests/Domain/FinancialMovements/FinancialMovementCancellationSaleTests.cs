using System;
using FluentAssertions;
using Mype.Domain.FinancialMovements;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Domain.Products;

namespace Mype.Tests.Domain.FinancialMovements
{
    public sealed class FinancialMovementCancellationSaleTests
    {
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 11, 14, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Cancel_Should_Cancel_Confirmed_Sale_And_Preserve_Product_Snapshot()
        {
            var businessId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var confirmerId = Guid.NewGuid();
            var cancellerId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var product = Product.Create(
                businessId,
                categoryId,
                "Producto",
                "PRODUCTO",
                8m,
                3m,
                creatorId,
                UtcNow.AddHours(-3)
            );
            var movement = FinancialMovement.CreateDraft(
                businessId,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 11),
                "Venta confirmada",
                "PEN",
                creatorId,
                UtcNow.AddHours(-3)
            );
            var active = movement.AddSaleItem(product, 2m, 8m, creatorId, UtcNow.AddHours(-2));
            var retired = movement.AddSaleItem(product, 1m, 8m, creatorId, UtcNow.AddHours(-2));
            movement.RetireItem(retired.Id, creatorId, UtcNow.AddMinutes(-90));
            movement.Confirm(confirmerId, UtcNow.AddHours(-1));
            var total = movement.TotalAmount;
            var activeVersion = active.Version;
            var retiredVersion = retired.Version;
            var confirmedAt = movement.ConfirmedAt;
            var reason = new string('a', FinancialMovementConstraints.CancellationReasonMinLength);

            movement.Cancel(reason, cancellerId, UtcNow);

            movement.Type.Should().Be(FinancialMovementType.Sale);
            movement.Status.Should().Be(FinancialMovementStatus.Cancelled);
            movement.TotalAmount.Should().Be(total);
            movement.ConfirmedAt.Should().Be(confirmedAt);
            movement.ConfirmedByUserId.Should().Be(confirmerId);
            active.ProductId.Should().Be(product.Id);
            active.CategoryId.Should().Be(categoryId);
            active.UnitCostSnapshot.Should().Be(product.UnitCost);
            active.Quantity.Should().Be(2m);
            active.UnitAmount.Should().Be(8m);
            active.SubtotalAmount.Should().Be(16m);
            active.IsActive.Should().BeTrue();
            active.Version.Should().Be(activeVersion);
            retired.ProductId.Should().Be(product.Id);
            retired.IsActive.Should().BeFalse();
            retired.Version.Should().Be(retiredVersion);
        }
    }
}
