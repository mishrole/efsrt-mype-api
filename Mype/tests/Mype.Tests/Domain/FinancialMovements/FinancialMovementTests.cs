using System;
using FluentAssertions;
using Mype.Domain.FinancialMovements;

namespace Mype.Tests.Domain.FinancialMovements
{
    public class FinancialMovementTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData(FinancialMovementType.Sale)]
        [InlineData(FinancialMovementType.Expense)]
        public void CreateDraft_Should_Initialize_Movement(FinancialMovementType type)
        {
            var movement = FinancialMovement.CreateDraft(
                BusinessId,
                type,
                new DateOnly(2026, 10, 10),
                "  Operación  ",
                "PEN",
                UserId,
                UtcNow
            );

            movement.Id.Should().NotBeEmpty();
            movement.BusinessId.Should().Be(BusinessId);
            movement.Type.Should().Be(type);
            movement.Status.Should().Be(FinancialMovementStatus.Draft);
            movement.MovementDate.Should().Be(new DateOnly(2026, 10, 10));
            movement.Description.Should().Be("Operación");
            movement.CurrencyCode.Should().Be("PEN");
            movement.TotalAmount.Should().Be(0m);
            movement.CreatedByUserId.Should().Be(UserId);
            movement.UpdatedByUserId.Should().Be(UserId);
            movement.CreatedAt.Should().Be(UtcNow);
            movement.UpdatedAt.Should().Be(UtcNow);
            movement.ConfirmedAt.Should().BeNull();
            movement.ConfirmedByUserId.Should().BeNull();
            movement.CancelledAt.Should().BeNull();
            movement.CancelledByUserId.Should().BeNull();
            movement.CancellationReason.Should().BeNull();
            movement.DiscardedAt.Should().BeNull();
            movement.IsEditable().Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateDraft_Should_Normalize_Empty_Description(string description)
        {
            var movement = FinancialMovement.CreateDraft(
                BusinessId,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 10),
                description,
                "PEN",
                UserId,
                UtcNow
            );
            movement.Description.Should().BeNull();
        }

        [Fact]
        public void UpdateDraftHeader_Should_Update_Only_Editable_Fields()
        {
            var movement = FinancialMovement.CreateDraft(
                BusinessId,
                FinancialMovementType.Sale,
                new DateOnly(2026, 10, 10),
                "Inicial",
                "PEN",
                UserId,
                UtcNow
            );
            var actorId = Guid.NewGuid();
            var updatedAt = UtcNow.AddHours(1);

            movement.UpdateDraftHeader(
                new DateOnly(2026, 10, 11),
                "  Actualizada  ",
                actorId,
                updatedAt
            );

            movement.MovementDate.Should().Be(new DateOnly(2026, 10, 11));
            movement.Description.Should().Be("Actualizada");
            movement.UpdatedByUserId.Should().Be(actorId);
            movement.UpdatedAt.Should().Be(updatedAt);
            movement.BusinessId.Should().Be(BusinessId);
            movement.Type.Should().Be(FinancialMovementType.Sale);
            movement.Status.Should().Be(FinancialMovementStatus.Draft);
            movement.CurrencyCode.Should().Be("PEN");
            movement.TotalAmount.Should().Be(0m);
            movement.CreatedByUserId.Should().Be(UserId);
            movement.CreatedAt.Should().Be(UtcNow);
        }
    }
}
