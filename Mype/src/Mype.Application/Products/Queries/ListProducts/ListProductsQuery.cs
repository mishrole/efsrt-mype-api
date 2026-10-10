using System;
using System.Collections.Generic;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.Products.Queries.ListProducts
{
    public sealed class ListProductsQuery
        : IRequest<Result<IReadOnlyCollection<ProductListItemResult>>>
    {
        public Guid BusinessId { get; set; }

        public Guid CurrentUserId { get; set; }

        public string Search { get; set; }

        public Guid? CategoryId { get; set; }

        public bool? IsActive { get; set; }

        public bool AvailableForSale { get; set; }
    }
}
