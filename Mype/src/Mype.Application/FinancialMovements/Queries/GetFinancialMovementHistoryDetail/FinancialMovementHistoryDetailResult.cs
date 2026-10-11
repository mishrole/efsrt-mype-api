using System;
using System.Collections.Generic;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Queries.GetFinancialMovementHistoryDetail
{
    public sealed record FinancialMovementHistoryDetailResult(
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
        decimal? EstimatedCost,
        decimal? EstimatedMargin,
        bool IsEditable,
        IReadOnlyCollection<string> AllowedActions,
        uint Version,
        IReadOnlyCollection<FinancialMovementHistoryItemDetailResult> Items,
        IReadOnlyCollection<FinancialMovementHistoryEvidenceResult> Evidences
    );
}
