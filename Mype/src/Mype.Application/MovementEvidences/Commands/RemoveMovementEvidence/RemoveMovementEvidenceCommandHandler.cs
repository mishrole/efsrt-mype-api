using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.MovementEvidences.Common;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Application.MovementEvidences.Models;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.MovementEvidences;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.MovementEvidences.Commands.RemoveMovementEvidence
{
    public sealed class RemoveMovementEvidenceCommandHandler
        : IRequestHandler<RemoveMovementEvidenceCommand, Result<MovementEvidenceResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IMovementEvidenceRepository _evidences;
        private readonly IEvidenceStorageSettings _settings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public RemoveMovementEvidenceCommandHandler(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            IFinancialMovementRepository movements,
            IMovementEvidenceRepository evidences,
            IEvidenceStorageSettings settings,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _memberships = memberships;
            _permissions = permissions;
            _movements = movements;
            _evidences = evidences;
            _settings = settings;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<MovementEvidenceResult>> Handle(
            RemoveMovementEvidenceCommand request,
            CancellationToken cancellationToken
        )
        {
            var accessError = await MovementEvidenceAuthorization.AuthorizeAsync(
                _memberships,
                _permissions,
                request.BusinessId,
                request.CurrentUserId,
                SystemPermissions.MovementUpdate.Code,
                cancellationToken
            );
            if (accessError != null)
                return Failure(accessError);

            var movement = await _movements.GetTrackedByIdAndBusinessAsync(
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            if (movement == null)
                return Failure(MovementEvidenceErrors.MovementNotFound);
            if (!movement.IsEditable())
                return Failure(MovementEvidenceErrors.MovementNotEditable);

            var evidence = await _evidences.GetTrackedByIdMovementAndBusinessAsync(
                request.EvidenceId,
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            if (evidence == null)
                return Failure(MovementEvidenceErrors.EvidenceNotFound);
            if (!evidence.IsActive())
                return Failure(MovementEvidenceErrors.EvidenceNotActive);

            var utcNow = _clock.UtcNow;
            _movements.SetOriginalVersion(movement, request.MovementVersion);
            _evidences.SetOriginalVersion(evidence, request.EvidenceVersion);
            movement.RegisterEvidenceChange(request.CurrentUserId, utcNow);
            evidence.MarkPendingPurge(
                EvidenceRemovalReason.Removed,
                request.CurrentUserId,
                utcNow,
                utcNow.AddDays(_settings.PurgeRetentionDays)
            );

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ApplicationErrorException exception)
                when (exception.Code == ErrorCodes.ConcurrencyConflict)
            {
                return Failure(MovementEvidenceErrors.ConcurrencyConflict);
            }
            catch
            {
                return Failure(MovementEvidenceErrors.OperationFailed);
            }

            return Result<MovementEvidenceResult>.Success(
                MovementEvidenceMapper.ToResult(evidence)
            );
        }

        private static Result<MovementEvidenceResult> Failure(ApplicationError error) =>
            Result<MovementEvidenceResult>.Failure(error);
    }
}
