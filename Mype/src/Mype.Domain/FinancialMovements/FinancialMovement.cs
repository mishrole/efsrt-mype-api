using Mype.Domain.Common;
using System;

namespace Mype.Domain.FinancialMovements
{
    public class FinancialMovement : Entity
    {
        private FinancialMovement() { }

        private FinancialMovement(
            Guid id,
            Guid businessId,
            FinancialMovementType type,
            DateOnly movementDate,
            string description,
            string currencyCode,
            Guid userId,
            DateTimeOffset utcNow
        ) : base(id)
        {
            BusinessId = businessId;
            Type = type;
            Status = FinancialMovementStatus.Draft;
            MovementDate = movementDate;
            Description = NormalizeOptionalValue(description);
            CurrencyCode = currencyCode;
            TotalAmount = 0m;
            CreatedByUserId = userId;
            UpdatedByUserId = userId;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public Guid BusinessId { get; private set; }
        public FinancialMovementType Type { get; private set; }
        public FinancialMovementStatus Status { get; private set; }
        public DateOnly MovementDate { get; private set; }
        public string Description { get; private set; }
        public string CurrencyCode { get; private set; } = string.Empty;
        public decimal TotalAmount { get; private set; }
        public Guid CreatedByUserId { get; private set; }
        public Guid UpdatedByUserId { get; private set; }
        public Guid? ConfirmedByUserId { get; private set; }
        public Guid? CancelledByUserId { get; private set; }
        public string CancellationReason { get; private set; }
        public DateTimeOffset? ConfirmedAt { get; private set; }
        public DateTimeOffset? CancelledAt { get; private set; }
        public DateTimeOffset? DiscardedAt { get; private set; }
        public uint Version { get; private set; }

        public bool IsEditable() => Status == FinancialMovementStatus.Draft;

        public void UpdateDraftHeader(
            DateOnly movementDate,
            string description,
            Guid currentUserId,
            DateTimeOffset utcNow
        )
        {
            MovementDate = movementDate;
            Description = NormalizeOptionalValue(description);
            UpdatedByUserId = currentUserId;
            UpdatedAt = utcNow;
        }

        public static FinancialMovement CreateDraft(
            Guid businessId,
            FinancialMovementType type,
            DateOnly movementDate,
            string description,
            string currencyCode,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            return new FinancialMovement(
                Guid.NewGuid(),
                businessId,
                type,
                movementDate,
                description,
                currencyCode,
                userId,
                utcNow
            );
        }

        private static string NormalizeOptionalValue(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
