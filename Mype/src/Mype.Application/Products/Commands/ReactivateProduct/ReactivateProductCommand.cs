using MediatR;
using Mype.Application.Common;
using Mype.Application.Products.Commands.Common;
using System;

namespace Mype.Application.Products.Commands.ReactivateProduct
{
    public sealed class ReactivateProductCommand : IRequest<Result<ProductMaintenanceResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid ProductId { get; set; }
        public Guid CurrentUserId { get; set; }
        public uint Version { get; set; }
    }
}
