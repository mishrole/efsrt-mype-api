using System;

namespace Mype.Application.ProductCategories
{
    public sealed record ProductCategoryResult(
        Guid Id,
        Guid BusinessId,
        string Name,
        string Color,
        bool IsDefault,
        bool IsActive,
        DateTimeOffset? DeactivatedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        uint Version
    );
}
