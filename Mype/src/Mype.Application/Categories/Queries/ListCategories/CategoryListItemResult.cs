using System;
using Mype.Domain.Categories;

namespace Mype.Application.Categories.Queries.ListCategories
{
    public sealed record CategoryListItemResult(
        Guid Id,
        Guid BusinessId,
        string Name,
        CategoryType Type,
        bool IsDefault,
        bool IsActive
    );
}
