using System;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Models
{
    public sealed record FinancialMovementHistoryItemProjection(
        Guid Id,
        FinancialMovementType Type,
        FinancialMovementStatus Status,
        DateOnly MovementDate,
        string Description,
        string CurrencyCode,
        decimal TotalAmount,
        int ActiveItemCount,
        bool HasActiveEvidence,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ConfirmedAt,
        DateTimeOffset? CancelledAt,
        DateTimeOffset? DiscardedAt
    );
}
