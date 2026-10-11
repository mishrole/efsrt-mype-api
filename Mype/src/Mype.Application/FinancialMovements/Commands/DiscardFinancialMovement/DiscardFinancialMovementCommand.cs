using System;
using System.Collections.Generic;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement
{
    public sealed class DiscardFinancialMovementCommand
        : IRequest<Result<DiscardFinancialMovementResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
        public uint MovementVersion { get; set; }
        public IReadOnlyCollection<MovementEvidenceVersion> EvidenceVersions { get; set; }
    }
}
