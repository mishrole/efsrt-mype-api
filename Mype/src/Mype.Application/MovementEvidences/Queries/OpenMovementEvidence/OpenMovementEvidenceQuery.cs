using System;
using MediatR;
using Mype.Application.Common;
using Mype.Application.MovementEvidences.Models;

namespace Mype.Application.MovementEvidences.Queries.OpenMovementEvidence
{
    public sealed class OpenMovementEvidenceQuery : IRequest<Result<TemporaryEvidenceAccessResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid EvidenceId { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}
