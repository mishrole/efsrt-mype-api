using System;
using MediatR;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Models;

namespace Mype.Application.FinancialMovements.Commands.UpdateSaleItem
{
    public sealed class UpdateSaleItemCommand
        : IRequest<Result<FinancialMovementItemMaintenanceResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
        public Guid ItemId { get; set; }
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitAmount { get; set; }
        public uint MovementVersion { get; set; }
        public uint ItemVersion { get; set; }
    }
}
