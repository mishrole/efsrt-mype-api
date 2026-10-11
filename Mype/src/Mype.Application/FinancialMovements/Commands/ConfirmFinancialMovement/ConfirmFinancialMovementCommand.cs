using System;
using System.Collections.Generic;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.FinancialMovements.Commands.ConfirmFinancialMovement
{
    public sealed class ConfirmFinancialMovementCommand
        : IRequest<Result<ConfirmFinancialMovementResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
        public uint MovementVersion { get; set; }
        public IReadOnlyCollection<FinancialMovementItemVersion> ItemVersions { get; set; }
    }
}
