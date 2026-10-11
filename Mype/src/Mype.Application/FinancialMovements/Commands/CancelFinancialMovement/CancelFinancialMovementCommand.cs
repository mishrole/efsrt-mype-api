using System;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.FinancialMovements.Commands.CancelFinancialMovement
{
    public sealed class CancelFinancialMovementCommand
        : IRequest<Result<CancelFinancialMovementResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
        public string Reason { get; set; }
        public uint MovementVersion { get; set; }
    }
}
