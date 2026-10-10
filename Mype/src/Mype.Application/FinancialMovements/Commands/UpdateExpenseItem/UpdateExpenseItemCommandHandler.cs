using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.FinancialMovements.Models;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.UpdateExpenseItem
{
    public sealed class UpdateExpenseItemCommandHandler
        : IRequestHandler<UpdateExpenseItemCommand, Result<ExpenseMovementItemMaintenanceResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly ICategoryRepository _categories;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public UpdateExpenseItemCommandHandler(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            IFinancialMovementRepository movements,
            ICategoryRepository categories,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _memberships = memberships;
            _permissions = permissions;
            _movements = movements;
            _categories = categories;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<ExpenseMovementItemMaintenanceResult>> Handle(
            UpdateExpenseItemCommand request,
            CancellationToken cancellationToken
        )
        {
            var accessError = await Authorize(
                request.BusinessId,
                request.CurrentUserId,
                cancellationToken
            );
            if (accessError != null)
                return Failure(accessError);
            var movement = await _movements.GetAggregateTrackedByIdAndBusinessAsync(
                request.MovementId,
                request.BusinessId,
                cancellationToken
            );
            if (movement == null)
                return Failure(UpdateExpenseItemErrors.MovementNotFound);
            if (!movement.IsEditable())
                return Failure(UpdateExpenseItemErrors.MovementNotEditable);
            if (movement.Type != FinancialMovementType.Expense)
                return Failure(UpdateExpenseItemErrors.MovementMustBeExpense);
            var category = await _categories.GetByIdAndBusinessAsync(
                request.CategoryId,
                request.BusinessId,
                cancellationToken
            );
            if (category == null)
                return Failure(UpdateExpenseItemErrors.CategoryNotFound);
            if (!category.IsActive || category.Type != CategoryType.Expense)
                return Failure(UpdateExpenseItemErrors.ExpenseCategoryUnavailable);
            var item = movement.Items.SingleOrDefault(current => current.Id == request.ItemId);
            if (item == null)
                return Failure(UpdateExpenseItemErrors.ItemNotFound);
            if (!item.IsActive)
                return Failure(UpdateExpenseItemErrors.ItemAlreadyRetired);
            _movements.SetOriginalVersions(
                movement,
                request.MovementVersion,
                item,
                request.ItemVersion
            );
            item = movement.UpdateExpenseItem(
                request.ItemId,
                category.Id,
                request.Description,
                request.Quantity,
                request.UnitAmount,
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
                return Failure(UpdateExpenseItemErrors.ConcurrencyConflict);
            }
            catch
            {
                return Failure(UpdateExpenseItemErrors.OperationFailed);
            }
            return Result<ExpenseMovementItemMaintenanceResult>.Success(
                new(ToResult(item, category.Name), movement.TotalAmount, movement.Version)
            );
        }

        private async Task<ApplicationError> Authorize(
            Guid businessId,
            Guid userId,
            CancellationToken cancellationToken
        )
        {
            var context = await _memberships.GetContextByBusinessAndUserAsync(
                businessId,
                userId,
                cancellationToken
            );
            if (
                context == null
                || context.MembershipStatus != BusinessMembershipStatus.Active
                || !context.RoleIsActive
            )
                return UpdateExpenseItemErrors.BusinessAccessForbidden;
            if (context.BusinessStatus != BusinessStatus.Active)
                return UpdateExpenseItemErrors.BusinessUnavailable;
            var codes = await _permissions.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );
            return codes.Contains(SystemPermissions.MovementUpdate.Code)
                ? null
                : UpdateExpenseItemErrors.MovementAccessForbidden;
        }

        private static ExpenseMovementItemResult ToResult(
            FinancialMovementItem item,
            string categoryName
        ) =>
            new(
                item.Id,
                item.Description,
                item.CategoryId,
                categoryName,
                item.Quantity,
                item.UnitAmount,
                item.SubtotalAmount,
                item.IsActive,
                item.RetiredAt,
                item.Version
            );

        private static Result<ExpenseMovementItemMaintenanceResult> Failure(
            ApplicationError error
        ) => Result<ExpenseMovementItemMaintenanceResult>.Failure(error);
    }
}
