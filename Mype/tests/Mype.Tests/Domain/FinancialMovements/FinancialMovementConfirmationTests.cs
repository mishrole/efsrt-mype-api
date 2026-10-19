using System;
using System.Reflection;
using FluentAssertions;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Products;

namespace Mype.Tests.Domain.FinancialMovements
{
    public sealed class FinancialMovementConfirmationTests
    {
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 10, 20, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Confirm_Should_Confirm_Sale_And_Calculate_Cost_And_Margin()
        {
            var userId = Guid.NewGuid();
            var movement = CreateMovement(FinancialMovementType.Sale, userId);
            var product = CreateProduct(movement.BusinessId, 2.20m, userId);
            var item = movement.AddSaleItem(product, 3m, 4m, userId, UtcNow.AddMinutes(1));

            var result = movement.Confirm(userId, UtcNow.AddMinutes(2));

            movement.Status.Should().Be(FinancialMovementStatus.Confirmed);
            movement.TotalAmount.Should().Be(12m);
            movement.ConfirmedByUserId.Should().Be(userId);
            movement.ConfirmedAt.Should().Be(UtcNow.AddMinutes(2));
            movement.UpdatedByUserId.Should().Be(userId);
            movement.UpdatedAt.Should().Be(UtcNow.AddMinutes(2));
            item.SubtotalAmount.Should().Be(12m);
            result.EstimatedCost.Should().Be(6.60m);
            result.EstimatedMargin.Should().Be(5.40m);
        }

        [Fact]
        public void Confirm_Should_Confirm_Expense_With_Null_Cost_And_Margin()
        {
            var userId = Guid.NewGuid();
            var movement = CreateMovement(FinancialMovementType.Expense, userId);
            movement.AddExpenseItem(Guid.NewGuid(), "Bolsas", 2m, 15.50m, userId, UtcNow);

            var result = movement.Confirm(userId, UtcNow.AddMinutes(1));

            movement.Status.Should().Be(FinancialMovementStatus.Confirmed);
            movement.TotalAmount.Should().Be(31m);
            result.EstimatedCost.Should().BeNull();
            result.EstimatedMargin.Should().BeNull();
        }

        [Fact]
        public void Confirm_Should_Exclude_Retired_Items()
        {
            var userId = Guid.NewGuid();
            var movement = CreateMovement(FinancialMovementType.Expense, userId);
            movement.AddExpenseItem(Guid.NewGuid(), "Activo", 2m, 10m, userId, UtcNow);
            var retired = movement.AddExpenseItem(
                Guid.NewGuid(),
                "Retirado",
                5m,
                10m,
                userId,
                UtcNow
            );
            movement.RetireItem(retired.Id, userId, UtcNow.AddMinutes(1));

            movement.Confirm(userId, UtcNow.AddMinutes(2));

            movement.TotalAmount.Should().Be(20m);
            retired.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Confirm_Should_Allow_Zero_Total()
        {
            var userId = Guid.NewGuid();
            var movement = CreateMovement(FinancialMovementType.Expense, userId);
            movement.AddExpenseItem(Guid.NewGuid(), "Sin costo", 1m, 0m, userId, UtcNow);

            movement.Confirm(userId, UtcNow.AddMinutes(1));

            movement.TotalAmount.Should().Be(0m);
            movement.Status.Should().Be(FinancialMovementStatus.Confirmed);
        }

        [Fact]
        public void Confirm_Should_Reject_Movement_Without_Active_Items()
        {
            var movement = CreateMovement(FinancialMovementType.Expense, Guid.NewGuid());

            var action = () => movement.Confirm(Guid.NewGuid(), UtcNow);

            action
                .Should()
                .Throw<FinancialMovementConfirmationException>()
                .Which.Error.Should()
                .Be(FinancialMovementConfirmationError.MovementRequiresActiveItems);
        }

        [Fact]
        public void Confirm_Should_Reject_Already_Confirmed_Movement()
        {
            var userId = Guid.NewGuid();
            var movement = CreateMovement(FinancialMovementType.Expense, userId);
            movement.AddExpenseItem(Guid.NewGuid(), "Bolsas", 1m, 1m, userId, UtcNow);
            movement.Confirm(userId, UtcNow.AddMinutes(1));

            var action = () => movement.Confirm(userId, UtcNow.AddMinutes(2));

            action
                .Should()
                .Throw<FinancialMovementConfirmationException>()
                .Which.Error.Should()
                .Be(FinancialMovementConfirmationError.MovementNotEditable);
            movement.ConfirmedAt.Should().Be(UtcNow.AddMinutes(1));
        }

        [Fact]
        public void Confirm_Should_Reject_Invalid_Sale_Item_And_Identify_Item()
        {
            var userId = Guid.NewGuid();
            var movement = CreateMovement(FinancialMovementType.Sale, userId);
            var item = movement.AddSaleItem(
                CreateProduct(movement.BusinessId, 2m, userId),
                1m,
                3m,
                userId,
                UtcNow
            );
            SetProperty(item, nameof(FinancialMovementItem.UnitCostSnapshot), (decimal?)null);

            var action = () => movement.Confirm(userId, UtcNow.AddMinutes(1));

            var exception = action.Should().Throw<FinancialMovementConfirmationException>().Which;
            exception.Error.Should().Be(FinancialMovementConfirmationError.InvalidSaleItem);
            exception.ItemId.Should().Be(item.Id);
        }

        [Fact]
        public void Confirm_Should_Reject_Invalid_Expense_Item_And_Identify_Item()
        {
            var userId = Guid.NewGuid();
            var movement = CreateMovement(FinancialMovementType.Expense, userId);
            var item = movement.AddExpenseItem(Guid.NewGuid(), "Bolsas", 1m, 3m, userId, UtcNow);
            SetProperty(item, nameof(FinancialMovementItem.ProductId), Guid.NewGuid());

            var action = () => movement.Confirm(userId, UtcNow.AddMinutes(1));

            var exception = action.Should().Throw<FinancialMovementConfirmationException>().Which;
            exception.Error.Should().Be(FinancialMovementConfirmationError.InvalidExpenseItem);
            exception.ItemId.Should().Be(item.Id);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        public void Confirm_Should_Reject_Invalid_Amounts(decimal quantity, decimal unitAmount)
        {
            var userId = Guid.NewGuid();
            var movement = CreateMovement(FinancialMovementType.Expense, userId);
            var item = movement.AddExpenseItem(Guid.NewGuid(), "Bolsas", 1m, 1m, userId, UtcNow);
            SetProperty(item, nameof(FinancialMovementItem.Quantity), quantity);
            SetProperty(item, nameof(FinancialMovementItem.UnitAmount), unitAmount);

            var action = () => movement.Confirm(userId, UtcNow.AddMinutes(1));

            var exception = action.Should().Throw<FinancialMovementConfirmationException>().Which;
            exception.Error.Should().Be(FinancialMovementConfirmationError.InvalidItemAmounts);
            exception.ItemId.Should().Be(item.Id);
        }

        [Fact]
        public void Confirm_Should_Block_Further_Header_Item_And_Evidence_Changes()
        {
            var userId = Guid.NewGuid();
            var movement = CreateMovement(FinancialMovementType.Expense, userId);
            var item = movement.AddExpenseItem(Guid.NewGuid(), "Bolsas", 1m, 1m, userId, UtcNow);
            movement.Confirm(userId, UtcNow.AddMinutes(1));

            FluentActions
                .Invoking(() =>
                    movement.UpdateDraftHeader(new DateOnly(2026, 10, 11), null, userId, UtcNow)
                )
                .Should()
                .Throw<FinancialMovementItemException>();
            FluentActions
                .Invoking(() => movement.RetireItem(item.Id, userId, UtcNow))
                .Should()
                .Throw<FinancialMovementItemException>();
            FluentActions
                .Invoking(() => movement.RegisterEvidenceChange(userId, UtcNow))
                .Should()
                .Throw<FinancialMovementItemException>();
        }

        private static FinancialMovement CreateMovement(FinancialMovementType type, Guid userId) =>
            FinancialMovement.CreateDraft(
                Guid.NewGuid(),
                type,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                userId,
                UtcNow
            );

        private static Product CreateProduct(Guid businessId, decimal unitCost, Guid userId) =>
            Product.Create(
                businessId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Gaseosa",
                "GASEOSA",
                4m,
                unitCost,
                userId,
                UtcNow
            );

        private static void SetProperty<T>(object instance, string propertyName, T value)
        {
            var property = instance
                .GetType()
                .GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );
            property.Should().NotBeNull();
            property.SetValue(instance, value);
        }
    }
}
