using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.MovementEvidences.Common;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Application.MovementEvidences.Models;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Permissions.Constants;

namespace Mype.Application.MovementEvidences.Queries.OpenMovementEvidence
{
    public sealed class OpenMovementEvidenceQueryHandler
        : IRequestHandler<OpenMovementEvidenceQuery, Result<TemporaryEvidenceAccessResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IMovementEvidenceRepository _evidences;
        private readonly IEvidenceStorageSettings _settings;
        private readonly IObjectStorage _storage;

        public OpenMovementEvidenceQueryHandler(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            IFinancialMovementRepository movements,
            IMovementEvidenceRepository evidences,
            IEvidenceStorageSettings settings,
            IObjectStorage storage
        )
        {
            _memberships = memberships;
            _permissions = permissions;
            _movements = movements;
            _evidences = evidences;
            _settings = settings;
            _storage = storage;
        }

        public async Task<Result<TemporaryEvidenceAccessResult>> Handle(
            OpenMovementEvidenceQuery request,
            CancellationToken cancellationToken
        )
        {
            var accessError = await MovementEvidenceAuthorization.AuthorizeAsync(
                _memberships,
                _permissions,
                request.BusinessId,
                request.CurrentUserId,
                SystemPermissions.MovementRead.Code,
                cancellationToken
            );
            if (accessError != null)
                return Result<TemporaryEvidenceAccessResult>.Failure(accessError);

            var movement = await _movements.GetTrackedByIdAndBusinessAsync(
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            if (movement == null)
                return Result<TemporaryEvidenceAccessResult>.Failure(
                    MovementEvidenceErrors.MovementNotFound
                );

            var evidence = await _evidences.GetTrackedByIdMovementAndBusinessAsync(
                request.EvidenceId,
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            if (evidence == null)
                return Result<TemporaryEvidenceAccessResult>.Failure(
                    MovementEvidenceErrors.EvidenceNotFound
                );
            if (!evidence.IsActive())
                return Result<TemporaryEvidenceAccessResult>.Failure(
                    MovementEvidenceErrors.EvidenceNotActive
                );

            try
            {
                var access = await _storage.CreateTemporaryReadAccessAsync(
                    evidence.StorageObjectKey,
                    TimeSpan.FromMinutes(_settings.ReadUriMinutes),
                    cancellationToken
                );
                return Result<TemporaryEvidenceAccessResult>.Success(
                    new(access.Url, access.ExpiresAt)
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return Result<TemporaryEvidenceAccessResult>.Failure(
                    MovementEvidenceErrors.StorageError
                );
            }
        }
    }
}
