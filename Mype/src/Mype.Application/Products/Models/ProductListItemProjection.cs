using System;

namespace Mype.Application.Products.Models
{
    public sealed record ProductListItemProjection(
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
