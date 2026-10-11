using System;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Commands.ConfirmFinancialMovement
{
    public sealed record ConfirmFinancialMovementResult(
        Guid Id,
        Guid BusinessId,
        FinancialMovementType Type,
        FinancialMovementStatus Status,
        DateOnly MovementDate,
        string CurrencyCode,
        decimal TotalAmount,
        decimal? EstimatedCost,
        decimal? EstimatedMargin,
        Guid ConfirmedByUserId,
        DateTimeOffset ConfirmedAt,
        uint Version
    );
}
