using System;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.ProductCategories.Queries.GetProductCategoryDetail
{
    public sealed class GetProductCategoryDetailQuery : IRequest<Result<ProductCategoryResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid ProductCategoryId { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}
