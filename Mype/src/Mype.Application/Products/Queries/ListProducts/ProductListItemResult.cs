using System;

namespace Mype.Application.Products.Queries.ListProducts
{
    public sealed record ProductListItemResult(
        Guid Id,
        Guid BusinessId,
        Guid CategoryId,
        string CategoryName,
        string Name,
        decimal SalePrice,
        decimal UnitCost,
        bool IsActive
    );
}