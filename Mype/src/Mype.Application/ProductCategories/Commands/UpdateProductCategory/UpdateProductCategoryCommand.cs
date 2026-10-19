using System;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.ProductCategories.Commands.UpdateProductCategory
{
    public sealed class UpdateProductCategoryCommand : IRequest<Result<ProductCategoryResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid ProductCategoryId { get; set; }
        public Guid CurrentUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public uint Version { get; set; }
    }
}
