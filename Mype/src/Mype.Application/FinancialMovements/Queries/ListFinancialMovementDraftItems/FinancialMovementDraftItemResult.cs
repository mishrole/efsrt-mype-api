using System;

namespace Mype.Application.FinancialMovements.Queries.ListFinancialMovementDraftItems
{
    public sealed record FinancialMovementDraftItemResult(
        Guid Id,
        Guid CategoryId,
        string CategoryName,
        Guid? ProductId,
        Guid? ProductCategoryId,
        string ProductName,
        string Description,
        decimal Quantity,
        decimal UnitAmount,
        decimal? UnitCostSnapshot,
        decimal SubtotalAmount,
        decimal? EstimatedCost,
        decimal? EstimatedMargin,
        uint Version
    );
}
