using System;
using System.Collections.Generic;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Models
{
    public sealed record FinancialMovementHistoryDetailProjection(
        Guid Id,
        Guid BusinessId,
        FinancialMovementType Type,
        FinancialMovementStatus Status,
        DateOnly MovementDate,
        string Description,
        string CurrencyCode,
        decimal TotalAmount,
        Guid CreatedByUserId,
        Guid UpdatedByUserId,
        Guid? ConfirmedByUserId,
        Guid? CancelledByUserId,
        string CancellationReason,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? ConfirmedAt,
        DateTimeOffset? CancelledAt,
        DateTimeOffset? DiscardedAt,
        uint Version,
        IReadOnlyCollection<FinancialMovementHistoryItemDetailProjection> Items,
        IReadOnlyCollection<FinancialMovementHistoryEvidenceProjection> Evidences
    );
}
