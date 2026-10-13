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
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.Businesses.Commands.ReplaceOpeningBalance
{
    public sealed class ReplaceOpeningBalanceCommandHandler
        : IRequestHandler<ReplaceOpeningBalanceCommand, Result<ReplaceOpeningBalanceResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public ReplaceOpeningBalanceCommandHandler(
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

        public async Task<Result<ReplaceOpeningBalanceResult>> Handle(
            ReplaceOpeningBalanceCommand request,
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
                return Result<ReplaceOpeningBalanceResult>.Failure(
                    new(
                        ErrorCodes.BusinessAccessForbidden,
                        ErrorMessages.BusinessAccessForbidden,
                        ApplicationErrorType.Forbidden
                    )
                );
            var permissions = await _permissions.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );
            if (!permissions.Contains(SystemPermissions.OpeningBalanceReplace.Code))
                return Result<ReplaceOpeningBalanceResult>.Failure(
                    new(
                        ErrorCodes.MovementAccessForbidden,
                        ErrorMessages.MovementAccessForbidden,
                        ApplicationErrorType.Forbidden
                    )
                );
            var current = await _movements.GetCurrentOpeningBalanceTrackedAsync(
                request.BusinessId,
                cancellationToken
            );
            if (current == null)
                return Result<ReplaceOpeningBalanceResult>.Failure(
                    new(
                        ErrorCodes.MovementNotFound,
                        ErrorMessages.MovementNotFound,
                        ApplicationErrorType.NotFound
                    )
                );
            _movements.SetOriginalVersion(current, request.MovementVersion);
            var now = _clock.UtcNow;
            var replacement = FinancialMovement.CreateOpeningBalance(
                request.BusinessId,
                current.MovementDate,
                request.Amount,
                current.CurrencyCode,
                request.CurrentUserId,
                now,
                current.Id,
                request.Reason.Trim()
            );
            current.SupersedeOpeningBalance(request.CurrentUserId, now);
            await _movements.AddAsync(replacement, cancellationToken);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ApplicationErrorException e) when (e.Code == ErrorCodes.ConcurrencyConflict)
            {
                return Result<ReplaceOpeningBalanceResult>.Failure(
                    new(
                        ErrorCodes.ConcurrencyConflict,
                        ErrorMessages.ConcurrencyConflict,
                        ApplicationErrorType.Conflict
                    )
                );
            }
            catch
            {
                return Result<ReplaceOpeningBalanceResult>.Failure(
                    new(
                        ErrorCodes.BusinessCreationFailed,
                        ErrorMessages.BusinessCreationFailed,
                        ApplicationErrorType.Internal
                    )
                );
            }
            return Result<ReplaceOpeningBalanceResult>.Success(
                new(
                    replacement.Id,
                    replacement.TotalAmount,
                    replacement.MovementDate,
                    replacement.Version
                )
            );
        }
    }
}
