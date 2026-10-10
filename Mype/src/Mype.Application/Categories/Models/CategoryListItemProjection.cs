using System;
using Mype.Domain.Categories;

namespace Mype.Application.Categories.Models
{
    public sealed record CategoryListItemProjection(
        Guid Id,
        Guid BusinessId,
        string Name,
        CategoryType Type,
        bool IsDefault,
        bool IsActive
    );
}
