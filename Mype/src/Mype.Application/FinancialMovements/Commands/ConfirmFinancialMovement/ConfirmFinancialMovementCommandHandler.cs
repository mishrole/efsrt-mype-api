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
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.ConfirmFinancialMovement
{
    public sealed class ConfirmFinancialMovementCommandHandler
        : IRequestHandler<ConfirmFinancialMovementCommand, Result<ConfirmFinancialMovementResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public ConfirmFinancialMovementCommandHandler(
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

        public async Task<Result<ConfirmFinancialMovementResult>> Handle(
            ConfirmFinancialMovementCommand request,
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
                return Failure(ConfirmFinancialMovementErrors.BusinessAccessForbidden);
            if (context.BusinessStatus != BusinessStatus.Active)
                return Failure(ConfirmFinancialMovementErrors.BusinessUnavailable);

            var permissions = await _permissions.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );
            if (!permissions.Contains(SystemPermissions.MovementConfirm.Code))
                return Failure(ConfirmFinancialMovementErrors.MovementAccessForbidden);

            var movement = await _movements.GetAggregateTrackedByIdAndBusinessAsync(
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            if (movement == null)
                return Failure(ConfirmFinancialMovementErrors.MovementNotFound);
            if (!movement.IsEditable())
                return Failure(ConfirmFinancialMovementErrors.MovementNotEditable);

            var activeItems = movement.Items.Where(item => item.IsActive).ToArray();
            if (!TryBuildItemVersions(activeItems, request.ItemVersions, out var itemVersions))
                return Failure(ConfirmFinancialMovementErrors.InvalidItemVersions);

            _movements.SetOriginalVersions(movement, request.MovementVersion, itemVersions);

            FinancialMovementConfirmation confirmation;
            try
            {
                confirmation = movement.Confirm(request.CurrentUserId, _clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (FinancialMovementConfirmationException exception)
            {
                return Failure(MapDomainError(exception.Error));
            }
            catch (ApplicationErrorException exception)
                when (exception.Code == ErrorCodes.ConcurrencyConflict)
            {
                return Failure(ConfirmFinancialMovementErrors.ConcurrencyConflict);
            }
            catch
            {
                return Failure(ConfirmFinancialMovementErrors.OperationFailed);
            }

            return Result<ConfirmFinancialMovementResult>.Success(
                new(
                    movement.Id,
                    movement.BusinessId,
                    movement.Type,
                    movement.Status,
                    movement.MovementDate,
                    movement.CurrencyCode,
                    confirmation.TotalAmount,
                    confirmation.EstimatedCost,
                    confirmation.EstimatedMargin,
                    confirmation.ConfirmedByUserId,
                    confirmation.ConfirmedAt,
                    movement.Version
                )
            );
        }

        private static bool TryBuildItemVersions(
            IReadOnlyCollection<FinancialMovementItem> activeItems,
            IReadOnlyCollection<FinancialMovementItemVersion> suppliedVersions,
            out IReadOnlyDictionary<Guid, uint> itemVersions
        )
        {
            itemVersions = null;
            if (suppliedVersions == null || suppliedVersions.Count != activeItems.Count)
                return false;

            var grouped = suppliedVersions.GroupBy(version => version.ItemId).ToArray();
            if (grouped.Any(group => group.Count() != 1))
                return false;

            var versions = grouped.ToDictionary(
                group => group.Key,
                group => group.Single().Version
            );
            if (activeItems.Any(item => !versions.ContainsKey(item.Id)))
                return false;

            itemVersions = versions;
            return true;
        }

        private static ApplicationError MapDomainError(FinancialMovementConfirmationError error) =>
            error switch
            {
                FinancialMovementConfirmationError.MovementNotEditable =>
                    ConfirmFinancialMovementErrors.MovementNotEditable,
                FinancialMovementConfirmationError.MovementRequiresActiveItems =>
                    ConfirmFinancialMovementErrors.RequiresActiveItems,
                FinancialMovementConfirmationError.InvalidSaleItem =>
                    ConfirmFinancialMovementErrors.InvalidSaleItems,
                FinancialMovementConfirmationError.InvalidExpenseItem =>
                    ConfirmFinancialMovementErrors.InvalidExpenseItems,
                _ => ConfirmFinancialMovementErrors.InvalidMovementItems,
            };

        private static Result<ConfirmFinancialMovementResult> Failure(ApplicationError error) =>
            Result<ConfirmFinancialMovementResult>.Failure(error);
    }
}
