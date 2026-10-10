using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;

namespace Mype.Application.FinancialMovements.Commands.CreateFinancialMovement
{
    public sealed class CreateFinancialMovementCommandHandler
        : IRequestHandler<CreateFinancialMovementCommand, Result<CreateFinancialMovementResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public CreateFinancialMovementCommandHandler(
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

        public async Task<Result<CreateFinancialMovementResult>> Handle(
            CreateFinancialMovementCommand request,
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
                return Failure(CreateFinancialMovementErrors.BusinessAccessForbidden);
            if (context.BusinessStatus != BusinessStatus.Active)
                return Failure(CreateFinancialMovementErrors.BusinessUnavailable);

            var permissions = await _permissions.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );
            if (!permissions.Contains(SystemPermissions.MovementCreate.Code))
                return Failure(CreateFinancialMovementErrors.MovementAccessForbidden);

            var movement = FinancialMovement.CreateDraft(
                request.BusinessId,
                request.Type,
                request.MovementDate,
                request.Description,
                context.CurrencyCode,
                request.CurrentUserId,
                _clock.UtcNow
            );
            await _movements.AddAsync(movement, cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return Failure(CreateFinancialMovementErrors.MovementCreationFailed);
            }

            return Result<CreateFinancialMovementResult>.Success(
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

        private static Result<CreateFinancialMovementResult> Failure(ApplicationError error) =>
            Result<CreateFinancialMovementResult>.Failure(error);
    }
}
