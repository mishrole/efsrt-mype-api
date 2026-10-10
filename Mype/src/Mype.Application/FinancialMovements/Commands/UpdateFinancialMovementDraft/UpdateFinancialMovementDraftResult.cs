using System;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft
{
    public sealed record UpdateFinancialMovementDraftResult(
        Guid Id,
        Guid BusinessId,
        FinancialMovementType Type,
        FinancialMovementStatus Status,
        DateOnly MovementDate,
        string Description,
        string CurrencyCode,
        decimal TotalAmount,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        uint Version
    );
}
