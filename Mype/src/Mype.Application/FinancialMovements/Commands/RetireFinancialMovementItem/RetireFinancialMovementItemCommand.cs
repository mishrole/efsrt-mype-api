using System;
using MediatR;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Models;

namespace Mype.Application.FinancialMovements.Commands.RetireFinancialMovementItem
{
    public sealed class RetireFinancialMovementItemCommand
        : IRequest<Result<RetiredFinancialMovementItemResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
        public Guid ItemId { get; set; }
        public uint MovementVersion { get; set; }
        public uint ItemVersion { get; set; }
    }
}
