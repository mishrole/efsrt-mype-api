using System;
using System.Collections.Generic;
using System.Linq;
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
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.MovementEvidences;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement
{
    public sealed class DiscardFinancialMovementCommandHandler
        : IRequestHandler<DiscardFinancialMovementCommand, Result<DiscardFinancialMovementResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IMovementEvidenceRepository _evidences;
        private readonly IEvidenceStorageSettings _settings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public DiscardFinancialMovementCommandHandler(
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

        public async Task<Result<DiscardFinancialMovementResult>> Handle(
            DiscardFinancialMovementCommand request,
            CancellationToken cancellationToken
        )
        {
            var accessError = await MovementEvidenceAuthorization.AuthorizeAsync(
                _memberships,
                _permissions,
                request.BusinessId,
                request.CurrentUserId,
                SystemPermissions.MovementDiscard.Code,
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
                return Failure(DiscardFinancialMovementErrors.MovementNotFound);
            if (!movement.IsEditable())
                return Failure(DiscardFinancialMovementErrors.MovementNotEditable);

            var activeEvidences = await _evidences.ListActiveTrackedByMovementAndBusinessAsync(
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            if (
                !TryBuildEvidenceVersions(
                    activeEvidences,
                    request.EvidenceVersions,
                    out var versions
                )
            )
                return Failure(DiscardFinancialMovementErrors.InvalidEvidenceVersions);

            _movements.SetOriginalVersion(movement, request.MovementVersion);
            _evidences.SetOriginalVersions(activeEvidences, versions);

            var utcNow = _clock.UtcNow;
            var purgeAfter = utcNow.AddDays(_settings.PurgeRetentionDays);
            movement.Discard(request.CurrentUserId, utcNow);
            foreach (var evidence in activeEvidences)
            {
                evidence.MarkPendingPurge(
                    EvidenceRemovalReason.MovementDiscarded,
                    request.CurrentUserId,
                    utcNow,
                    purgeAfter
                );
            }

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
                return Failure(DiscardFinancialMovementErrors.ConcurrencyConflict);
            }
            catch
            {
                return Failure(DiscardFinancialMovementErrors.OperationFailed);
            }

            return Result<DiscardFinancialMovementResult>.Success(
                new(
                    movement.Id,
                    movement.BusinessId,
                    movement.Type,
                    movement.Status,
                    movement.MovementDate,
                    movement.TotalAmount,
                    movement.DiscardedAt.Value,
                    movement.Version,
                    activeEvidences.Select(ToResult).ToArray()
                )
            );
        }

        private static bool TryBuildEvidenceVersions(
            IReadOnlyCollection<MovementEvidence> activeEvidences,
            IReadOnlyCollection<MovementEvidenceVersion> suppliedVersions,
            out IReadOnlyDictionary<Guid, uint> versions
        )
        {
            versions = null;
            if (suppliedVersions == null || suppliedVersions.Count != activeEvidences.Count)
                return false;

            var grouped = suppliedVersions.GroupBy(version => version.EvidenceId).ToArray();
            if (grouped.Any(group => group.Count() != 1))
                return false;

            var supplied = grouped.ToDictionary(
                group => group.Key,
                group => group.Single().Version
            );
            if (activeEvidences.Any(evidence => !supplied.ContainsKey(evidence.Id)))
                return false;

            versions = supplied;
            return true;
        }

        private static DiscardedEvidenceResult ToResult(MovementEvidence evidence) =>
            new(
                evidence.Id,
                evidence.Status,
                evidence.RemovalReason,
                evidence.MarkedForPurgeAt,
                evidence.PurgeAfter,
                evidence.Version
            );

        private static Result<DiscardFinancialMovementResult> Failure(ApplicationError error) =>
            Result<DiscardFinancialMovementResult>.Failure(error);
    }
}
