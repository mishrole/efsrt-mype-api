using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.CancelFinancialMovement
{
    public sealed class CancelFinancialMovementCommandHandler
        : IRequestHandler<CancelFinancialMovementCommand, Result<CancelFinancialMovementResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public CancelFinancialMovementCommandHandler(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            IFinancialMovementRepository movements,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _memberships = memberships;
            _permissions = permissions;
            _movements = movements;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<CancelFinancialMovementResult>> Handle(
            CancelFinancialMovementCommand request,
            CancellationToken cancellationToken
        )
        {
            var context = await _memberships.GetContextByBusinessAndUserAsync(
                request.BusinessId,
                request.CurrentUserId,
                cancellationToken
            );
            if (
                context == null
                || context.MembershipStatus != BusinessMembershipStatus.Active
                || !context.RoleIsActive
            )
                return Failure(CancelFinancialMovementErrors.BusinessAccessForbidden);
            if (context.BusinessStatus != BusinessStatus.Active)
                return Failure(CancelFinancialMovementErrors.BusinessUnavailable);
            var permissions = await _permissions.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );
            if (!permissions.Contains(SystemPermissions.MovementCancel.Code))
                return Failure(CancelFinancialMovementErrors.MovementAccessForbidden);
            var movement = await _movements.GetTrackedByIdAndBusinessAsync(
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            if (movement == null)
                return Failure(CancelFinancialMovementErrors.MovementNotFound);
            if (movement.Status != FinancialMovementStatus.Confirmed)
                return Failure(CancelFinancialMovementErrors.MovementNotEditable);
            _movements.SetOriginalVersion(movement, request.MovementVersion);
            try
            {
                movement.Cancel(
                    CancellationReasonNormalizer.Normalize(request.Reason),
                    request.CurrentUserId,
                    _clock.UtcNow
                );
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (FinancialMovementCancellationException)
            {
                return Failure(CancelFinancialMovementErrors.MovementNotEditable);
            }
            catch (ApplicationErrorException exception)
                when (exception.Code == ErrorCodes.ConcurrencyConflict)
            {
                return Failure(CancelFinancialMovementErrors.ConcurrencyConflict);
            }
            catch
            {
                return Failure(CancelFinancialMovementErrors.OperationFailed);
            }
            return Result<CancelFinancialMovementResult>.Success(
                new(
                    movement.Id,
                    movement.BusinessId,
                    movement.Type,
                    movement.Status,
                    movement.MovementDate,
                    movement.CurrencyCode,
                    movement.TotalAmount,
                    movement.ConfirmedByUserId.Value,
                    movement.ConfirmedAt.Value,
                    movement.CancelledByUserId.Value,
                    movement.CancelledAt.Value,
                    movement.CancellationReason,
                    movement.Version
                )
            );
        }

        private static Result<CancelFinancialMovementResult> Failure(ApplicationError error) =>
            Result<CancelFinancialMovementResult>.Failure(error);
    }
}
