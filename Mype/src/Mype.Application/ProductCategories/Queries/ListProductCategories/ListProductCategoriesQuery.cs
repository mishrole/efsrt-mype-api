using System;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.ProductCategories.Queries.ListProductCategories
{
    public sealed class ListProductCategoriesQuery
        : IRequest<Result<System.Collections.Generic.IReadOnlyCollection<ProductCategoryResult>>>
    {
        public Guid BusinessId { get; set; }
        public Guid CurrentUserId { get; set; }
        public string Search { get; set; }
        public bool? IsActive { get; set; }
    }
}
