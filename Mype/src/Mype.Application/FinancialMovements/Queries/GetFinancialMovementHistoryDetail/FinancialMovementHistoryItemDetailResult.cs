using System;

namespace Mype.Application.FinancialMovements.Queries.GetFinancialMovementHistoryDetail
{
    public sealed record FinancialMovementHistoryItemDetailResult(
        Guid Id,
        Guid CategoryId,
        string CategoryName,
        bool? CategoryIsActive,
        Guid? ProductId,
        string ProductName,
        bool? ProductIsActive,
        string Description,
        decimal Quantity,
        decimal UnitAmount,
        decimal? UnitCostSnapshot,
        decimal SubtotalAmount,
        bool IsActive,
        DateTimeOffset? RetiredAt,
        uint Version
    );
}
