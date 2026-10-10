using Mype.Domain.FinancialMovements;
using System;
namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementDrafts
{
    public sealed record FinancialMovementDraftListItemResult(
        Guid Id, FinancialMovementType Type, DateOnly MovementDate,
        string Description, string CurrencyCode, decimal TotalAmount,
        DateTimeOffset UpdatedAt, uint Version
    );
}
