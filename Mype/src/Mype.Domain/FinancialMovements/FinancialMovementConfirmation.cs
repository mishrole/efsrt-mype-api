using System;

namespace Mype.Domain.FinancialMovements
{
    public sealed record FinancialMovementConfirmation(
        decimal TotalAmount,
        decimal? EstimatedCost,
        decimal? EstimatedMargin,
        Guid ConfirmedByUserId,
        DateTimeOffset ConfirmedAt
    );
}
