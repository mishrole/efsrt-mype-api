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
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft
{
    public sealed class UpdateFinancialMovementDraftCommandHandler
        : IRequestHandler<
            UpdateFinancialMovementDraftCommand,
            Result<UpdateFinancialMovementDraftResult>
        >
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public UpdateFinancialMovementDraftCommandHandler(
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

        public async Task<Result<UpdateFinancialMovementDraftResult>> Handle(
            UpdateFinancialMovementDraftCommand request,
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
                return Failure(UpdateFinancialMovementDraftErrors.BusinessAccessForbidden);
            if (context.BusinessStatus != BusinessStatus.Active)
                return Failure(UpdateFinancialMovementDraftErrors.BusinessUnavailable);

            var permissions = await _permissions.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );
            if (!permissions.Contains(SystemPermissions.MovementUpdate.Code))
                return Failure(UpdateFinancialMovementDraftErrors.MovementAccessForbidden);

            var movement = await _movements.GetTrackedByIdAndBusinessAsync(
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            if (movement == null)
                return Failure(UpdateFinancialMovementDraftErrors.MovementNotFound);
            if (!movement.IsEditable())
                return Failure(UpdateFinancialMovementDraftErrors.MovementNotEditable);

            _movements.SetOriginalVersion(movement, request.Version);
            movement.UpdateDraftHeader(
                request.MovementDate,
                request.Description,
                request.CurrentUserId,
                _clock.UtcNow
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
                return Failure(UpdateFinancialMovementDraftErrors.MovementConcurrencyConflict);
            }
            catch
            {
                return Failure(UpdateFinancialMovementDraftErrors.MovementUpdateFailed);
            }

            return Result<UpdateFinancialMovementDraftResult>.Success(
                new(
                    movement.Id,
                    movement.BusinessId,
                    movement.Type,
                    movement.Status,
                    movement.MovementDate,
                    movement.Description,
                    movement.CurrencyCode,
                    movement.TotalAmount,
                    movement.CreatedAt,
                    movement.UpdatedAt,
                    movement.Version
                )
            );
        }

        private static Result<UpdateFinancialMovementDraftResult> Failure(ApplicationError error) =>
            Result<UpdateFinancialMovementDraftResult>.Failure(error);
    }
}
