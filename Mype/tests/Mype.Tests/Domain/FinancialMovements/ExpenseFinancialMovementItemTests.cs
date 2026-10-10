using System;
using System.Reflection;
using FluentAssertions;
using Mype.Domain.FinancialMovements;
using Mype.Domain.FinancialMovements.Constraints;

namespace Mype.Tests.Domain.FinancialMovements
{
    public sealed class ExpenseFinancialMovementItemTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid CategoryId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void AddExpenseItem_Should_Normalize_Description_Keep_Product_Fields_Null_And_Recalculate_Total()
        {
            var movement = ExpenseMovement();

            var item = movement.AddExpenseItem(
                CategoryId,
                "  Bolsas reforzadas  ",
                2.5000m,
                15.50m,
                UserId,
                UtcNow
            );

            item.Description.Should().Be("Bolsas reforzadas");
            item.CategoryId.Should().Be(CategoryId);
            item.ProductId.Should().BeNull();
            item.UnitCostSnapshot.Should().BeNull();
            item.Quantity.Should().Be(2.5000m);
            item.UnitAmount.Should().Be(15.50m);
            item.SubtotalAmount.Should().Be(38.75m);
            item.EstimatedCost.Should().Be(0m);
            item.EstimatedMargin.Should().Be(38.75m);
            item.IsActive.Should().BeTrue();
            movement.TotalAmount.Should().Be(38.75m);
        }

        [Fact]
        public void AddExpenseItem_Should_Allow_Fractional_Quantity_And_Zero_UnitAmount()
        {
            var movement = ExpenseMovement();

            var item = movement.AddExpenseItem(CategoryId, "Muestra", 0.1250m, 0m, UserId, UtcNow);

            item.SubtotalAmount.Should().Be(0m);
            movement.TotalAmount.Should().Be(0m);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddExpenseItem_Should_Reject_Invalid_Description(string description)
        {
            var action = () =>
                ExpenseMovement().AddExpenseItem(CategoryId, description, 1m, 1m, UserId, UtcNow);

            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.InvalidDescription);
        }

        [Fact]
        public void AddExpenseItem_Should_Reject_Description_Above_Maximum_Length()
        {
            var description = new string(
                'a',
                FinancialMovementItemConstraints.DescriptionMaxLength + 1
            );
            var action = () =>
                ExpenseMovement().AddExpenseItem(CategoryId, description, 1m, 1m, UserId, UtcNow);

            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.DescriptionTooLong);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void AddExpenseItem_Should_Reject_Invalid_Quantity(decimal quantity)
        {
            var action = () =>
                ExpenseMovement()
                    .AddExpenseItem(CategoryId, "Concepto", quantity, 1m, UserId, UtcNow);

            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.InvalidQuantity);
        }

        [Fact]
        public void AddExpenseItem_Should_Reject_Negative_UnitAmount()
        {
            var action = () =>
                ExpenseMovement()
                    .AddExpenseItem(CategoryId, "Concepto", 1m, -0.01m, UserId, UtcNow);

            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.InvalidUnitAmount);
        }

        [Fact]
        public void AddExpenseItem_Should_Reject_Sale_Movement()
        {
            var movement = FinancialMovement.CreateDraft(
                BusinessId,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                UserId,
                UtcNow
            );
            var action = () =>
                movement.AddExpenseItem(CategoryId, "Concepto", 1m, 1m, UserId, UtcNow);

            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.MovementMustBeExpense);
        }

        [Fact]
        public void UpdateExpenseItem_Should_Replace_Fields_And_Recalculate_Total()
        {
            var movement = ExpenseMovement();
            var item = movement.AddExpenseItem(CategoryId, "Inicial", 2m, 4m, UserId, UtcNow);
            var replacementCategoryId = Guid.NewGuid();

            movement.UpdateExpenseItem(
                item.Id,
                replacementCategoryId,
                "  Actualizado  ",
                3m,
                5m,
                UserId,
                UtcNow.AddMinutes(1)
            );

            item.CategoryId.Should().Be(replacementCategoryId);
            item.Description.Should().Be("Actualizado");
            item.ProductId.Should().BeNull();
            item.UnitCostSnapshot.Should().BeNull();
            item.SubtotalAmount.Should().Be(15m);
            movement.TotalAmount.Should().Be(15m);
        }

        [Fact]
        public void UpdateExpenseItem_Should_Preserve_Amounts_When_Only_Description_Changes()
        {
            var movement = ExpenseMovement();
            var item = movement.AddExpenseItem(
                CategoryId,
                "Inicial",
                2.2500m,
                4.50m,
                UserId,
                UtcNow
            );

            movement.UpdateExpenseItem(
                item.Id,
                CategoryId,
                "Nuevo texto",
                item.Quantity,
                item.UnitAmount,
                UserId,
                UtcNow.AddMinutes(1)
            );

            item.Quantity.Should().Be(2.2500m);
            item.UnitAmount.Should().Be(4.50m);
            item.SubtotalAmount.Should().Be(10.13m);
        }

        [Fact]
        public void RetireItem_Should_Keep_Expense_Item_And_Set_Total_To_Zero()
        {
            var movement = ExpenseMovement();
            var item = movement.AddExpenseItem(CategoryId, "Concepto", 2m, 4m, UserId, UtcNow);

            movement.RetireItem(item.Id, UserId, UtcNow.AddMinutes(1));

            item.IsActive.Should().BeFalse();
            item.RetiredAt.Should().Be(UtcNow.AddMinutes(1));
            movement.Items.Should().ContainSingle();
            movement.TotalAmount.Should().Be(0m);
            movement.Status.Should().Be(FinancialMovementStatus.Draft);
        }

        [Fact]
        public void UpdateExpenseItem_Should_Reject_Retired_Item()
        {
            var movement = ExpenseMovement();
            var item = movement.AddExpenseItem(CategoryId, "Concepto", 1m, 1m, UserId, UtcNow);
            movement.RetireItem(item.Id, UserId, UtcNow);
            var action = () =>
                movement.UpdateExpenseItem(
                    item.Id,
                    CategoryId,
                    "Cambio",
                    1m,
                    1m,
                    UserId,
                    UtcNow.AddMinutes(1)
                );

            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.ItemAlreadyRetired);
        }

        [Fact]
        public void ExpenseItem_Operations_Should_Reject_Closed_Movement()
        {
            var movement = ExpenseMovement();
            SetStatus(movement, FinancialMovementStatus.Confirmed);
            var action = () =>
                movement.AddExpenseItem(CategoryId, "Concepto", 1m, 1m, UserId, UtcNow);

            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.MovementNotEditable);
        }

        [Fact]
        public void UpdateExpenseItem_Should_Not_Mutate_When_Amounts_Are_Invalid()
        {
            var movement = ExpenseMovement();
            var item = movement.AddExpenseItem(
                CategoryId,
                "Descripción original",
                2m,
                4m,
                UserId,
                UtcNow
            );
            var originalTotal = movement.TotalAmount;

            var action = () =>
                movement.UpdateExpenseItem(
                    item.Id,
                    Guid.NewGuid(),
                    "Descripción modificada",
                    0m,
                    8m,
                    UserId,
                    UtcNow.AddMinutes(1)
                );

            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.InvalidQuantity);
            item.CategoryId.Should().Be(CategoryId);
            item.Description.Should().Be("Descripción original");
            item.Quantity.Should().Be(2m);
            item.UnitAmount.Should().Be(4m);
            item.SubtotalAmount.Should().Be(8m);
            movement.TotalAmount.Should().Be(originalTotal);
        }

        private static FinancialMovement ExpenseMovement() =>
            FinancialMovement.CreateDraft(
                BusinessId,
                FinancialMovementType.Expense,
                new DateOnly(2026, 10, 10),
                null,
                "PEN",
                UserId,
                UtcNow
            );

        private static void SetStatus(FinancialMovement movement, FinancialMovementStatus status) =>
            typeof(FinancialMovement)
                .GetProperty(
                    nameof(FinancialMovement.Status),
                    BindingFlags.Instance | BindingFlags.Public
                )!
                .SetValue(movement, status);
    }
}
