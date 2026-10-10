using System;
using Mype.Domain.Categories;

namespace Mype.Application.Products.Models
{
    public sealed record ProductDetailProjection(
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
