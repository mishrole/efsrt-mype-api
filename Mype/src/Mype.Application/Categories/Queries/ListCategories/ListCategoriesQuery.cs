using MediatR;
using Mype.Application.Common;
using Mype.Domain.Categories;
using System;
using System.Collections.Generic;

namespace Mype.Application.Categories.Queries.ListCategories
{
    public sealed class ListCategoriesQuery :
        IRequest<Result<IReadOnlyCollection<CategoryListItemResult>>>
    {
        public Guid BusinessId { get; set; }

        public Guid CurrentUserId { get; set; }

        public CategoryType? Type { get; set; }

        public bool? IsActive { get; set; }
    }
}
