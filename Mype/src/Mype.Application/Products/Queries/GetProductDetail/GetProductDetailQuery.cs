using MediatR;
using Mype.Application.Common;
using System;

namespace Mype.Application.Products.Queries.GetProductDetail
{
    public sealed class GetProductDetailQuery
        : IRequest<
            Result<ProductDetailResult>
        >
    {
        public Guid BusinessId { get; set; }

        public Guid ProductId { get; set; }

        public Guid CurrentUserId { get; set; }
    }
}