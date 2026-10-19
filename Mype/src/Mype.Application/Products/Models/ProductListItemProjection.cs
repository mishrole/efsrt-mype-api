using System;

namespace Mype.Application.Products.Models
{
    public sealed record ProductListItemProjection(
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
