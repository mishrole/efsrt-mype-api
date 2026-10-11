using System;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Commands.CancelFinancialMovement
{
    public sealed record CancelFinancialMovementResult(
        Guid Id,
        Guid BusinessId,
        FinancialMovementType Type,
        FinancialMovementStatus Status,
        DateOnly MovementDate,
        string CurrencyCode,
        decimal TotalAmount,
        Guid ConfirmedByUserId,
        DateTimeOffset ConfirmedAt,
        Guid CancelledByUserId,
        DateTimeOffset CancelledAt,
        string CancellationReason,
        uint Version
    );
}
