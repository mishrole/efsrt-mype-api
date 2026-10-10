using System;
using MediatR;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Models;

namespace Mype.Application.FinancialMovements.Commands.AddExpenseItem
{
    public sealed class AddExpenseItemCommand
        : IRequest<Result<ExpenseMovementItemMaintenanceResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
        public Guid CategoryId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitAmount { get; set; }
        public uint MovementVersion { get; set; }
    }
}
