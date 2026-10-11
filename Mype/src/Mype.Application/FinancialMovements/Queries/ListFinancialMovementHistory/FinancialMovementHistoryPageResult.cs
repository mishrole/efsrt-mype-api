using System.Collections.Generic;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementHistory
{
    public sealed record FinancialMovementHistoryPageResult(
        IReadOnlyCollection<FinancialMovementHistoryItemResult> Items,
        int Page,
        int PageSize,
        long TotalCount,
        bool HasPreviousPage,
        bool HasNextPage
    );
}
