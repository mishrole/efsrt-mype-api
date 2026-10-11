using System.Collections.Generic;

namespace Mype.Application.FinancialMovements.Models
{
    public sealed record FinancialMovementHistoryPageProjection(
        IReadOnlyCollection<FinancialMovementHistoryItemProjection> Items,
        int Page,
        int PageSize,
        long TotalCount
    );
}
