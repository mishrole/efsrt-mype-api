using System;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementHistory
{
    public sealed record FinancialMovementHistoryItemResult(
        Guid Id,
        FinancialMovementType Type,
        FinancialMovementStatus Status,
        DateOnly MovementDate,
        string Description,
        string CurrencyCode,
        decimal TotalAmount,
        int ActiveItemCount,
        bool HasActiveEvidence,
        DateTimeOffset? ConfirmedAt,
        DateTimeOffset? CancelledAt,
        DateTimeOffset? DiscardedAt
    );
}
