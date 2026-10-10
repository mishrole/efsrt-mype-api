using System;
using MediatR;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Models;

namespace Mype.Application.FinancialMovements.Commands.AddSaleItem
{
    public sealed class AddSaleItemCommand
        : IRequest<Result<FinancialMovementItemMaintenanceResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitAmount { get; set; }
        public uint MovementVersion { get; set; }
    }
}
