using System;
using MediatR;
using Mype.Application.Common;
using Mype.Application.MovementEvidences.Models;

namespace Mype.Application.MovementEvidences.Commands.AttachMovementEvidence
{
    public sealed class AttachMovementEvidenceCommand : IRequest<Result<MovementEvidenceResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
        public uint MovementVersion { get; set; }
        public EvidenceFile File { get; set; }
    }
}
