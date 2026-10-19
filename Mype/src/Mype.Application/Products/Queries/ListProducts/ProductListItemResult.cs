using System;

namespace Mype.Application.Products.Queries.ListProducts
{
    public sealed record ProductListItemResult(
        Guid Id,
        Guid BusinessId,
        Guid CategoryId,
        string CategoryName,
        Guid ProductCategoryId,
        string ProductCategoryName,
        string ProductCategoryColor,
        string Name,
        decimal SalePrice,
        decimal UnitCost,
        bool IsActive
    );
}
