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
using Mype.Application.Products.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.FinancialMovements.Commands.AddSaleItem
{
    public sealed class AddSaleItemCommandHandler
        : IRequestHandler<AddSaleItemCommand, Result<FinancialMovementItemMaintenanceResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IProductRepository _products;
        private readonly ICategoryRepository _categories;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public AddSaleItemCommandHandler(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            IFinancialMovementRepository movements,
            IProductRepository products,
            ICategoryRepository categories,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _memberships = memberships;
            _permissions = permissions;
            _movements = movements;
            _products = products;
            _categories = categories;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<FinancialMovementItemMaintenanceResult>> Handle(
            AddSaleItemCommand r,
            CancellationToken t
        )
        {
            var access = await Authorize(r, t);
            if (access != null)
                return Failure(access);
            var m = await _movements.GetAggregateTrackedByIdAndBusinessAsync(
                r.MovementId,
                r.BusinessId,
                t
            );
            if (m == null)
                return Failure(AddSaleItemErrors.MovementNotFound);
            if (!m.IsEditable())
                return Failure(AddSaleItemErrors.MovementNotEditable);
            if (m.Type != Domain.FinancialMovements.FinancialMovementType.Sale)
                return Failure(AddSaleItemErrors.MovementMustBeSale);
            var p = await _products.GetTrackedByIdAndBusinessAsync(r.ProductId, r.BusinessId, t);
            if (p == null)
                return Failure(AddSaleItemErrors.ProductNotFound);
            if (!p.IsActive)
                return Failure(AddSaleItemErrors.ProductInactive);
            var c = await _categories.GetByIdAndBusinessAsync(p.CategoryId, r.BusinessId, t);
            if (c == null || !c.IsActive || c.Type != CategoryType.Sale)
                return Failure(AddSaleItemErrors.ProductCategoryUnavailable);
            _movements.SetOriginalVersion(m, r.MovementVersion);
            var i = m.AddSaleItem(p, r.Quantity, r.UnitAmount, r.CurrentUserId, _clock.UtcNow);
            try
            {
                await _unitOfWork.SaveChangesAsync(t);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ApplicationErrorException ex) when (ex.Code == ErrorCodes.ConcurrencyConflict)
            {
                return Failure(AddSaleItemErrors.ConcurrencyConflict);
            }
            catch
            {
                return Failure(AddSaleItemErrors.OperationFailed);
            }
            return Result<FinancialMovementItemMaintenanceResult>.Success(
                new(ToItem(i, c.Name), m.TotalAmount, m.Version)
            );
        }

        private async Task<ApplicationError> Authorize(AddSaleItemCommand r, CancellationToken t)
        {
            var c = await _memberships.GetContextByBusinessAndUserAsync(
                r.BusinessId,
                r.CurrentUserId,
                t
            );
            if (
                c == null
                || c.MembershipStatus != BusinessMembershipStatus.Active
                || !c.RoleIsActive
            )
                return AddSaleItemErrors.BusinessAccessForbidden;
            if (c.BusinessStatus != BusinessStatus.Active)
                return AddSaleItemErrors.BusinessUnavailable;
            var p = await _permissions.ListActiveCodesByRoleIdAsync(c.RoleId, t);
            return p.Contains(SystemPermissions.MovementUpdate.Code)
                ? null
                : AddSaleItemErrors.MovementAccessForbidden;
        }

        private static FinancialMovementItemResult ToItem(
            Domain.FinancialMovements.FinancialMovementItem i,
            string category
        ) =>
            new(
                i.Id,
                i.ProductId.Value,
                i.Description,
                i.CategoryId,
                category,
                i.Quantity,
                i.UnitAmount,
                i.UnitCostSnapshot.Value,
                i.SubtotalAmount,
                i.EstimatedCost,
                i.EstimatedMargin,
                i.IsActive,
                i.RetiredAt,
                i.Version
            );

        private static Result<FinancialMovementItemMaintenanceResult> Failure(ApplicationError e) =>
            Result<FinancialMovementItemMaintenanceResult>.Failure(e);
    }
}
