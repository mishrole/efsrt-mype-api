using Mype.Domain.FinancialMovements;
using System;

namespace Mype.Application.FinancialMovements.Models
{
    public sealed record FinancialMovementDraftProjection(
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
