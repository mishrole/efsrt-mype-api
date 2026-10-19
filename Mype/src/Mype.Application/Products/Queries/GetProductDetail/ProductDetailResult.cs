using System;
using Mype.Domain.Categories;

namespace Mype.Application.Products.Queries.GetProductDetail
{
    public sealed record ProductDetailResult(
        Guid Id,
        Guid BusinessId,
        Guid CategoryId,
        string CategoryName,
        CategoryType CategoryType,
        Guid ProductCategoryId,
        string ProductCategoryName,
        string ProductCategoryColor,
        string Name,
        decimal SalePrice,
        decimal UnitCost,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        uint Version
    );
}
