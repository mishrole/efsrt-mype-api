using System;
using System.Reflection;
using FluentAssertions;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Products;

namespace Mype.Tests.Domain.FinancialMovements
{
    public class FinancialMovementItemTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid CategoryId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void AddSaleItem_Should_Copy_Snapshot_Calculate_Amounts_And_Total()
        {
            var movement = SaleMovement();
            var product = Product.Create(
                BusinessId,
                CategoryId,
                "Gaseosa",
                "GASEOSA",
                3.50m,
                2.20m,
                UserId,
                UtcNow
            );
            var item = movement.AddSaleItem(product, 10m, 3.50m, UserId, UtcNow.AddMinutes(1));
            item.ProductId.Should().Be(product.Id);
            item.CategoryId.Should().Be(CategoryId);
            item.Description.Should().Be("Gaseosa");
            item.UnitCostSnapshot.Should().Be(2.20m);
            item.SubtotalAmount.Should().Be(35m);
            item.EstimatedCost.Should().Be(22m);
            item.EstimatedMargin.Should().Be(13m);
            item.IsActive.Should().BeTrue();
            movement.TotalAmount.Should().Be(35m);
            movement.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
        }

        [Fact]
        public void AddSaleItem_Should_Allow_Zero_UnitAmount()
        {
            var movement = SaleMovement();
            var item = movement.AddSaleItem(CreateProduct(), 2m, 0m, UserId, UtcNow);
            item.SubtotalAmount.Should().Be(0m);
            movement.TotalAmount.Should().Be(0m);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void AddSaleItem_Should_Reject_Invalid_Quantity(decimal quantity)
        {
            var action = () =>
                SaleMovement().AddSaleItem(CreateProduct(), quantity, 1m, UserId, UtcNow);
            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.InvalidQuantity);
        }

        [Fact]
        public void AddSaleItem_Should_Reject_Negative_UnitAmount()
        {
            var action = () =>
                SaleMovement().AddSaleItem(CreateProduct(), 1m, -0.01m, UserId, UtcNow);
            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.InvalidUnitAmount);
        }

        [Fact]
        public void AddSaleItem_Should_Reject_Product_From_Another_Business()
        {
            var product = Product.Create(
                Guid.NewGuid(),
                CategoryId,
                "Otro",
                "OTRO",
                1m,
                1m,
                UserId,
                UtcNow
            );
            var action = () => SaleMovement().AddSaleItem(product, 1m, 1m, UserId, UtcNow);
            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.ProductBusinessMismatch);
        }

        [Fact]
        public void AddSaleItem_Should_Reject_Expense_Movement()
        {
            var movement = FinancialMovement.CreateDraft(
                BusinessId,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                UserId,
                UtcNow
            );
            var action = () => movement.AddSaleItem(CreateProduct(), 1m, 1m, UserId, UtcNow);
            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.MovementMustBeSale);
        }

        [Fact]
        public void UpdateSaleItem_Should_Replace_Snapshot_And_Recalculate_Total()
        {
            var movement = SaleMovement();
            var item = movement.AddSaleItem(CreateProduct(), 1m, 2m, UserId, UtcNow);
            var categoryId = Guid.NewGuid();
            var replacement = Product.Create(
                BusinessId,
                categoryId,
                "Agua",
                "AGUA",
                4m,
                1.50m,
                UserId,
                UtcNow
            );
            movement.UpdateSaleItem(item.Id, replacement, 3m, 4m, UserId, UtcNow.AddMinutes(1));
            item.ProductId.Should().Be(replacement.Id);
            item.CategoryId.Should().Be(categoryId);
            item.Description.Should().Be("Agua");
            item.UnitCostSnapshot.Should().Be(1.50m);
            item.SubtotalAmount.Should().Be(12m);
            movement.TotalAmount.Should().Be(12m);
        }

        [Fact]
        public void RetireItem_Should_Keep_Record_And_Exclude_Subtotal()
        {
            var movement = SaleMovement();
            var item = movement.AddSaleItem(CreateProduct(), 2m, 4m, UserId, UtcNow);
            movement.RetireItem(item.Id, UserId, UtcNow.AddMinutes(1));
            item.IsActive.Should().BeFalse();
            item.RetiredAt.Should().Be(UtcNow.AddMinutes(1));
            movement.Items.Should().ContainSingle();
            movement.TotalAmount.Should().Be(0m);
        }

        [Fact]
        public void RetireItem_Should_Reject_Second_Retirement()
        {
            var movement = SaleMovement();
            var item = movement.AddSaleItem(CreateProduct(), 1m, 1m, UserId, UtcNow);
            movement.RetireItem(item.Id, UserId, UtcNow);
            var action = () => movement.RetireItem(item.Id, UserId, UtcNow.AddMinutes(1));
            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.ItemAlreadyRetired);
        }

        [Fact]
        public void Item_Operations_Should_Reject_Closed_Movement()
        {
            var movement = SaleMovement();
            SetStatus(movement, FinancialMovementStatus.Confirmed);
            var action = () => movement.AddSaleItem(CreateProduct(), 1m, 1m, UserId, UtcNow);
            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.MovementNotEditable);
        }

        private static FinancialMovement SaleMovement() =>
            FinancialMovement.CreateDraft(
                BusinessId,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                UserId,
                UtcNow
            );

        private static Product CreateProduct() =>
            Product.Create(
                BusinessId,
                CategoryId,
                "Gaseosa",
                "GASEOSA",
                3.50m,
                2.20m,
                UserId,
                UtcNow
            );

        private static void SetStatus(FinancialMovement movement, FinancialMovementStatus status) =>
            typeof(FinancialMovement)
                .GetProperty(
                    nameof(FinancialMovement.Status),
                    BindingFlags.Instance | BindingFlags.Public
                )
                .SetValue(movement, status);
    }
}
