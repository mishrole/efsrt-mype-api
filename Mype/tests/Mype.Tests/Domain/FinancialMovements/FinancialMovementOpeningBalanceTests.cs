using System;
using FluentAssertions;
using Mype.Domain.FinancialMovements;

namespace Mype.Tests.Domain.FinancialMovements
{
    public sealed class FinancialMovementOpeningBalanceTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly DateOnly OpeningDate = new(2026, 10, 1);
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData(2500.00)]
        [InlineData(0.00)]
        [InlineData(-500.00)]
        public void CreateOpeningBalance_Should_Create_Confirmed_Current_Movement(decimal amount)
        {
            var movement = FinancialMovement.CreateOpeningBalance(
                BusinessId,
                OpeningDate,
                amount,
                "PEN",
                UserId,
                UtcNow
            );

            movement.BusinessId.Should().Be(BusinessId);
            movement.Type.Should().Be(FinancialMovementType.OpeningBalance);
            movement.Status.Should().Be(FinancialMovementStatus.Confirmed);
            movement.MovementDate.Should().Be(OpeningDate);
            movement.TotalAmount.Should().Be(amount);
            movement.CurrencyCode.Should().Be("PEN");
            movement.ConfirmedByUserId.Should().Be(UserId);
            movement.ConfirmedAt.Should().Be(UtcNow);
            movement.IsCurrentOpeningBalance.Should().BeTrue();
            movement.ReplacesMovementId.Should().BeNull();
            movement.Items.Should().BeEmpty();
            movement.IsEditable().Should().BeFalse();
        }

        [Fact]
        public void CreateOpeningBalance_Should_Round_To_MonetaryPrecision()
        {
            var movement = FinancialMovement.CreateOpeningBalance(
                BusinessId,
                OpeningDate,
                10.125m,
                "PEN",
                UserId,
                UtcNow
            );
            movement.TotalAmount.Should().Be(10.13m);
        }

        [Fact]
        public void CreateReplacement_Should_Preserve_Link_Reason_And_Date()
        {
            var originalId = Guid.NewGuid();
            var replacement = FinancialMovement.CreateOpeningBalance(
                BusinessId,
                OpeningDate,
                1800m,
                "PEN",
                UserId,
                UtcNow,
                originalId,
                "  Conciliación inicial  "
            );

            replacement.ReplacesMovementId.Should().Be(originalId);
            replacement.ReplacementReason.Should().Be("Conciliación inicial");
            replacement.MovementDate.Should().Be(OpeningDate);
            replacement.IsCurrentOpeningBalance.Should().BeTrue();
        }

        [Fact]
        public void SupersedeOpeningBalance_Should_Mark_Previous_As_Not_Current_And_Audit_Update()
        {
            var movement = FinancialMovement.CreateOpeningBalance(
                BusinessId,
                OpeningDate,
                1000m,
                "PEN",
                UserId,
                UtcNow
            );
            var actorId = Guid.NewGuid();
            var updatedAt = UtcNow.AddHours(1);

            movement.SupersedeOpeningBalance(actorId, updatedAt);

            movement.IsCurrentOpeningBalance.Should().BeFalse();
            movement.UpdatedByUserId.Should().Be(actorId);
            movement.UpdatedAt.Should().Be(updatedAt);
        }

        [Fact]
        public void SupersedeOpeningBalance_Should_Reject_Already_Superseded_Movement()
        {
            var movement = FinancialMovement.CreateOpeningBalance(
                BusinessId,
                OpeningDate,
                1000m,
                "PEN",
                UserId,
                UtcNow
            );
            movement.SupersedeOpeningBalance(UserId, UtcNow.AddMinutes(1));

            var action = () => movement.SupersedeOpeningBalance(UserId, UtcNow.AddMinutes(2));

            action.Should().Throw<InvalidOperationException>();
        }
    }
}
