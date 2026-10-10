using System;
using MediatR;
using Mype.Application.Common;
using Mype.Application.Products.Commands.Common;

namespace Mype.Application.Products.Commands.DeactivateProduct
{
    public sealed class DeactivateProductCommand : IRequest<Result<ProductMaintenanceResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid ProductId { get; set; }
        public Guid CurrentUserId { get; set; }
        public uint Version { get; set; }
    }
}
