using System;

namespace Mype.Application.FinancialMovements.Models
{
    public sealed record FinancialMovementHistoryItemDetailProjection(
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
