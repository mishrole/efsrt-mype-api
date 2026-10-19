using System;
using FluentAssertions;
using Mype.Domain.FinancialMovements;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Domain.Products;

namespace Mype.Tests.Domain.FinancialMovements
{
    public sealed class FinancialMovementCancellationTests
    {
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 11, 14, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData(FinancialMovementType.Sale)]
        [InlineData(FinancialMovementType.Expense)]
        public void Cancel_Should_Change_Confirmed_Movement_To_Cancelled(FinancialMovementType type)
        {
            var creatorId = Guid.NewGuid();
            var confirmerId = Guid.NewGuid();
            var cancellerId = Guid.NewGuid();
            var movement = CreateConfirmedMovement(type, creatorId, confirmerId);
            var confirmedAt = movement.ConfirmedAt;
            var confirmedBy = movement.ConfirmedByUserId;
            var total = movement.TotalAmount;
            var itemSnapshot = movement.Items;
            var reason = new string('a', FinancialMovementConstraints.CancellationReasonMinLength);

            movement.Cancel(reason, cancellerId, UtcNow);

            movement.Status.Should().Be(FinancialMovementStatus.Cancelled);
            movement.CancellationReason.Should().Be(reason);
            movement.CancelledByUserId.Should().Be(cancellerId);
            movement.CancelledAt.Should().Be(UtcNow);
            movement.UpdatedByUserId.Should().Be(cancellerId);
            movement.UpdatedAt.Should().Be(UtcNow);
            movement.ConfirmedByUserId.Should().Be(confirmedBy);
            movement.ConfirmedAt.Should().Be(confirmedAt);
            movement.TotalAmount.Should().Be(total);
            movement.Items.Should().BeEquivalentTo(itemSnapshot);
            movement.IsEditable().Should().BeFalse();
        }

        [Fact]
        public void Cancel_Should_Accept_Minimum_And_Maximum_Reason_Lengths()
        {
            var minimum = CreateConfirmedMovement(
                FinancialMovementType.Expense,
                Guid.NewGuid(),
                Guid.NewGuid()
            );
            var maximum = CreateConfirmedMovement(
                FinancialMovementType.Expense,
                Guid.NewGuid(),
                Guid.NewGuid()
            );

            minimum.Cancel(
                new string('a', FinancialMovementConstraints.CancellationReasonMinLength),
                Guid.NewGuid(),
                UtcNow
            );
            maximum.Cancel(
                new string('a', FinancialMovementConstraints.CancellationReasonMaxLength),
                Guid.NewGuid(),
                UtcNow
            );

            minimum.Status.Should().Be(FinancialMovementStatus.Cancelled);
            maximum.Status.Should().Be(FinancialMovementStatus.Cancelled);
        }

        [Theory]
        [InlineData(FinancialMovementStatus.Draft)]
        [InlineData(FinancialMovementStatus.Cancelled)]
        [InlineData(FinancialMovementStatus.Discarded)]
        public void Cancel_Should_Reject_NonConfirmed_Status(FinancialMovementStatus status)
        {
            var movement = CreateMovement(FinancialMovementType.Expense, Guid.NewGuid());
            if (status == FinancialMovementStatus.Cancelled)
            {
                movement.AddExpenseItem(Guid.NewGuid(), "Concepto", 1m, 2m, Guid.NewGuid(), UtcNow);
                movement.Confirm(Guid.NewGuid(), UtcNow.AddMinutes(1));
                movement.Cancel(
                    new string('a', FinancialMovementConstraints.CancellationReasonMinLength),
                    Guid.NewGuid(),
                    UtcNow.AddMinutes(2)
                );
            }
            else if (status == FinancialMovementStatus.Discarded)
            {
                movement.Discard(Guid.NewGuid(), UtcNow);
            }

            var action = () =>
                movement.Cancel(
                    new string('a', FinancialMovementConstraints.CancellationReasonMinLength),
                    Guid.NewGuid(),
                    UtcNow.AddHours(1)
                );

            action
                .Should()
                .Throw<FinancialMovementCancellationException>()
                .Which.Error.Should()
                .Be(FinancialMovementCancellationError.MovementNotConfirmed);
        }

        [Fact]
        public void Cancel_Should_Reject_Invalid_Reason_Lengths_And_Whitespace()
        {
            var invalidReasons = new[]
            {
                (string)null,
                string.Empty,
                "   ",
                new string('a', FinancialMovementConstraints.CancellationReasonMinLength - 1),
                new string('a', FinancialMovementConstraints.CancellationReasonMaxLength + 1),
                $" {new string('a', FinancialMovementConstraints.CancellationReasonMinLength)} ",
            };

            foreach (var reason in invalidReasons)
            {
                var movement = CreateConfirmedMovement(
                    FinancialMovementType.Expense,
                    Guid.NewGuid(),
                    Guid.NewGuid()
                );
                var action = () => movement.Cancel(reason, Guid.NewGuid(), UtcNow);

                action
                    .Should()
                    .Throw<FinancialMovementCancellationException>()
                    .Which.Error.Should()
                    .Be(FinancialMovementCancellationError.InvalidReason);
                movement.Status.Should().Be(FinancialMovementStatus.Confirmed);
                movement.CancelledAt.Should().BeNull();
                movement.CancelledByUserId.Should().BeNull();
                movement.CancellationReason.Should().BeNull();
            }
        }

        [Fact]
        public void Cancel_Should_Cancel_Confirmed_Sale_And_Preserve_Product_Snapshot()
        {
            var businessId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var confirmerId = Guid.NewGuid();
            var cancellerId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var productCategoryId = Guid.NewGuid();
            var product = Product.Create(
                businessId,
                categoryId,
                productCategoryId,
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

        private static FinancialMovement CreateConfirmedMovement(
            FinancialMovementType type,
            Guid creatorId,
            Guid confirmerId
        )
        {
            var movement = CreateMovement(type, creatorId);
            movement.AddExpenseItem(
                Guid.NewGuid(),
                "Concepto válido",
                2m,
                5m,
                creatorId,
                UtcNow.AddHours(-2)
            );
            movement.Confirm(confirmerId, UtcNow.AddHours(-1));
            return movement;
        }

        private static FinancialMovement CreateMovement(FinancialMovementType type, Guid userId) =>
            FinancialMovement.CreateDraft(
                Guid.NewGuid(),
                type == FinancialMovementType.Sale ? FinancialMovementType.Expense : type,
                new DateOnly(2026, 10, 11),
                "Movimiento histórico",
                "PEN",
                userId,
                UtcNow.AddHours(-3)
            );
    }
}
