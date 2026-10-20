using System;

namespace Mype.Application.FinancialMovements.Models
{
    public sealed record FinancialMovementDraftItemProjection(
        Guid Id,
        Guid CategoryId,
        string CategoryName,
        Guid? ProductId,
        Guid? ProductCategoryId,
        string Description,
        decimal Quantity,
        decimal UnitAmount,
        decimal? UnitCostSnapshot,
        decimal SubtotalAmount,
        uint Version
    );
}
