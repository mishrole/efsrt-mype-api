using Mype.Domain.Categories;
using System;

namespace Mype.Application.Products.Queries.GetProductDetail
{
    public sealed record ProductDetailResult(
        Guid Id,
        Guid BusinessId,
        Guid CategoryId,
        string CategoryName,
        CategoryType CategoryType,
        string Name,
        decimal SalePrice,
        decimal UnitCost,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        uint Version
    );
}