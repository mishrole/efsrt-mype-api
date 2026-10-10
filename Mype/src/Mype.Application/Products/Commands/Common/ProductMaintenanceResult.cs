using System;

namespace Mype.Application.Products.Commands.Common
{
    public sealed record ProductMaintenanceResult(
        Guid Id,
        Guid BusinessId,
        Guid CategoryId,
        string CategoryName,
        string Name,
        decimal SalePrice,
        decimal UnitCost,
        bool IsActive,
        DateTimeOffset? DeactivatedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        uint Version
    );
}
