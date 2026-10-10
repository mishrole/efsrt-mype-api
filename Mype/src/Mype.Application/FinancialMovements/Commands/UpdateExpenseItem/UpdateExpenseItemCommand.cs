using System;
using MediatR;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Models;

namespace Mype.Application.FinancialMovements.Commands.UpdateExpenseItem
{
    public sealed class UpdateExpenseItemCommand
        : IRequest<Result<ExpenseMovementItemMaintenanceResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid ItemId { get; set; }
        public Guid CurrentUserId { get; set; }
        public Guid CategoryId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitAmount { get; set; }
        public uint MovementVersion { get; set; }
        public uint ItemVersion { get; set; }
    }
}
