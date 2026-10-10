using MediatR;
using Mype.Application.Common;
using Mype.Application.Products.Commands.Common;
using System;

namespace Mype.Application.Products.Commands.UpdateProduct
{
    public sealed class UpdateProductCommand : IRequest<Result<ProductMaintenanceResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid ProductId { get; set; }
        public Guid CurrentUserId { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal SalePrice { get; set; }
        public decimal UnitCost { get; set; }
        public uint Version { get; set; }
    }
}
