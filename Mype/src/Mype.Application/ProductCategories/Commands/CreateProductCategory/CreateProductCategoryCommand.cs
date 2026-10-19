using System;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.ProductCategories.Commands.CreateProductCategory
{
    public sealed class CreateProductCategoryCommand : IRequest<Result<ProductCategoryResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid CurrentUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; }
    }
}
