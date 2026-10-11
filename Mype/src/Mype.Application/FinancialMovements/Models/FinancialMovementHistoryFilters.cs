using System;
using Mype.Domain.FinancialMovements;

namespace Mype.Application.FinancialMovements.Models
{
    public sealed record FinancialMovementHistoryFilters(
        FinancialMovementType? Type,
        FinancialMovementStatus? Status,
        DateOnly? DateFrom,
        DateOnly? DateTo,
        Guid? CategoryId,
        int Page,
        int PageSize
    );
}
