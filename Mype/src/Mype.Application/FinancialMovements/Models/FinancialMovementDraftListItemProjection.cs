using System;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Models
{
    public sealed record FinancialMovementDraftListItemProjection(
        Guid Id,
        FinancialMovementType Type,
        DateOnly MovementDate,
        string Description,
        string CurrencyCode,
        decimal TotalAmount,
        DateTimeOffset UpdatedAt,
        uint Version
    );
}
