using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mype.Domain.MovementEvidences;

namespace Mype.Application.MovementEvidences.Interfaces
{
    public interface IMovementEvidenceRepository
    {
        Task AddAsync(MovementEvidence evidence, CancellationToken cancellationToken);

        Task<MovementEvidence> GetTrackedByIdMovementAndBusinessAsync(
            Guid evidenceId,
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        );

        Task<IReadOnlyCollection<MovementEvidence>> ListActiveByMovementAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        );
        Task<IReadOnlyCollection<MovementEvidence>> ListActiveTrackedByMovementAndBusinessAsync(
            Guid movementId,
            Guid businessId,
            CancellationToken cancellationToken
        );
        void SetOriginalVersion(MovementEvidence evidence, uint version);
        void SetOriginalVersions(
            IReadOnlyCollection<MovementEvidence> evidences,
            IReadOnlyDictionary<Guid, uint> versions
        );
    }
}
