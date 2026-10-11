using System;
using MediatR;
using Mype.Application.Common;
using Mype.Application.MovementEvidences.Models;

namespace Mype.Application.MovementEvidences.Commands.ReplaceMovementEvidence
{
    public sealed class ReplaceMovementEvidenceCommand : IRequest<Result<MovementEvidenceResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid EvidenceId { get; set; }
        public Guid CurrentUserId { get; set; }
        public uint MovementVersion { get; set; }
        public uint EvidenceVersion { get; set; }
        public EvidenceFile File { get; set; }
    }
}
