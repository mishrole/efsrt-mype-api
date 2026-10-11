using System;
using System.Collections.Generic;
using MediatR;
using Mype.Application.Common;
using Mype.Application.MovementEvidences.Models;

namespace Mype.Application.MovementEvidences.Queries.ListMovementEvidences
{
    public sealed class ListMovementEvidencesQuery
        : IRequest<Result<IReadOnlyCollection<MovementEvidenceResult>>>
    {
        public Guid BusinessId { get; set; }
        public Guid MovementId { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}
