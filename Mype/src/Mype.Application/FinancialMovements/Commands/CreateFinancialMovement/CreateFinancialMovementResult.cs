using System;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Commands.CreateFinancialMovement
{
    public sealed record CreateFinancialMovementResult(
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
