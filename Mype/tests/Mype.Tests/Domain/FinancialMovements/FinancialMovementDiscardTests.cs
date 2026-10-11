using System;
using System.Reflection;
using FluentAssertions;
using Mype.Domain.FinancialMovements;

namespace Mype.Tests.Domain.FinancialMovements
{
    public sealed class FinancialMovementDiscardTests
    {
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData(FinancialMovementType.Sale)]
        [InlineData(FinancialMovementType.Expense)]
        public void Discard_Should_Change_Draft_To_Discarded(FinancialMovementType type)
        {
            var userId = Guid.NewGuid();
            var movement = CreateMovement(type, userId);
            var originalTotal = movement.TotalAmount;
            var createdAt = movement.CreatedAt;
            var createdBy = movement.CreatedByUserId;

            movement.Discard(userId, UtcNow.AddHours(1));

            movement.Status.Should().Be(FinancialMovementStatus.Discarded);
            movement.DiscardedAt.Should().Be(UtcNow.AddHours(1));
            movement.UpdatedAt.Should().Be(UtcNow.AddHours(1));
            movement.UpdatedByUserId.Should().Be(userId);
            movement.TotalAmount.Should().Be(originalTotal);
            movement.CreatedAt.Should().Be(createdAt);
            movement.CreatedByUserId.Should().Be(createdBy);
            movement.ConfirmedAt.Should().BeNull();
            movement.ConfirmedByUserId.Should().BeNull();
            movement.CancelledAt.Should().BeNull();
            movement.CancelledByUserId.Should().BeNull();
            movement.CancellationReason.Should().BeNull();
        }

        [Fact]
        public void Discard_Should_Preserve_Active_And_Retired_Items()
        {
            var userId = Guid.NewGuid();
            var movement = CreateMovement(FinancialMovementType.Expense, userId);
            var active = movement.AddExpenseItem(Guid.NewGuid(), "Activo", 2m, 10m, userId, UtcNow);
            var retired = movement.AddExpenseItem(
                Guid.NewGuid(),
                "Retirado",
                1m,
                5m,
                userId,
                UtcNow
            );
            movement.RetireItem(retired.Id, userId, UtcNow.AddMinutes(1));
            var total = movement.TotalAmount;

            movement.Discard(userId, UtcNow.AddHours(1));

            movement.Items.Should().HaveCount(2);
            active.IsActive.Should().BeTrue();
            retired.IsActive.Should().BeFalse();
            movement.TotalAmount.Should().Be(total);
        }

        [Fact]
        public void Discard_Should_Allow_Draft_Without_Items()
        {
            var movement = CreateMovement(FinancialMovementType.Sale, Guid.NewGuid());

            movement.Discard(Guid.NewGuid(), UtcNow);

            movement.Status.Should().Be(FinancialMovementStatus.Discarded);
            movement.Items.Should().BeEmpty();
        }

        [Theory]
        [InlineData(FinancialMovementStatus.Confirmed)]
        [InlineData(FinancialMovementStatus.Cancelled)]
        [InlineData(FinancialMovementStatus.Discarded)]
        public void Discard_Should_Reject_NonDraft_Status(FinancialMovementStatus status)
        {
            var movement = CreateMovement(FinancialMovementType.Sale, Guid.NewGuid());
            SetStatus(movement, status);

            var action = () => movement.Discard(Guid.NewGuid(), UtcNow);

            action
                .Should()
                .Throw<FinancialMovementItemException>()
                .Which.Error.Should()
                .Be(FinancialMovementItemError.MovementNotEditable);
        }

        private static FinancialMovement CreateMovement(FinancialMovementType type, Guid userId) =>
            FinancialMovement.CreateDraft(
                Guid.NewGuid(),
                type,
                new DateOnly(2026, 10, 11),
                null,
                "PEN",
                userId,
                UtcNow
            );

        private static void SetStatus(FinancialMovement movement, FinancialMovementStatus status) =>
            typeof(FinancialMovement)
                .GetProperty(
                    nameof(FinancialMovement.Status),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )
                .SetValue(movement, status);
    }
}
