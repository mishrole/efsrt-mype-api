using MediatR;
using Mype.Application.Common;
using System;

namespace Mype.Application.Products.Commands.CreateProduct
{
    public sealed class CreateProductCommand
        : IRequest<
            Result<CreateProductResult>
        >
    {
        public Guid BusinessId { get; set; }

        public Guid CurrentUserId { get; set; }

        public Guid CategoryId { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public decimal SalePrice { get; set; }

        public decimal UnitCost { get; set; }
    }
}