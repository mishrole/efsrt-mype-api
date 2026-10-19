using System;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.ProductCategories.Commands.ReactivateProductCategory
{
    public sealed class ReactivateProductCategoryCommand : IRequest<Result<ProductCategoryResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid ProductCategoryId { get; set; }
        public Guid CurrentUserId { get; set; }
        public uint Version { get; set; }
    }
}
