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

namespace Mype.Application.FinancialMovements.Commands.UpdateSaleItem
{
    public sealed class UpdateSaleItemCommandHandler
        : IRequestHandler<UpdateSaleItemCommand, Result<FinancialMovementItemMaintenanceResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IFinancialMovementRepository _movements;
        private readonly IProductRepository _products;
        private readonly ICategoryRepository _categories;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public UpdateSaleItemCommandHandler(
            IBusinessMembershipRepository a,
            IPermissionRepository b,
            IFinancialMovementRepository c,
            IProductRepository d,
            ICategoryRepository e,
            IUnitOfWork f,
            IClock g
        )
        {
            _memberships = a;
            _permissions = b;
            _movements = c;
            _products = d;
            _categories = e;
            _unitOfWork = f;
            _clock = g;
        }

        public async Task<Result<FinancialMovementItemMaintenanceResult>> Handle(
            UpdateSaleItemCommand r,
            CancellationToken t
        )
        {
            var x = await _memberships.GetContextByBusinessAndUserAsync(
                r.BusinessId,
                r.CurrentUserId,
                t
            );
            if (
                x == null
                || x.MembershipStatus != BusinessMembershipStatus.Active
                || !x.RoleIsActive
            )
                return F(UpdateSaleItemErrors.BusinessAccessForbidden);
            if (x.BusinessStatus != BusinessStatus.Active)
                return F(UpdateSaleItemErrors.BusinessUnavailable);
            if (
                !(await _permissions.ListActiveCodesByRoleIdAsync(x.RoleId, t)).Contains(
                    SystemPermissions.MovementUpdate.Code
                )
            )
                return F(UpdateSaleItemErrors.MovementAccessForbidden);
            var m = await _movements.GetAggregateTrackedByIdAndBusinessAsync(
                r.MovementId,
                r.BusinessId,
                t
            );
            if (m == null)
                return F(UpdateSaleItemErrors.MovementNotFound);
            if (!m.IsEditable())
                return F(UpdateSaleItemErrors.MovementNotEditable);
            if (m.Type != Domain.FinancialMovements.FinancialMovementType.Sale)
                return F(UpdateSaleItemErrors.MovementMustBeSale);
            var i = m.Items.SingleOrDefault(z => z.Id == r.ItemId);
            if (i == null)
                return F(UpdateSaleItemErrors.ItemNotFound);
            if (!i.IsActive)
                return F(UpdateSaleItemErrors.ItemAlreadyRetired);
            var p = await _products.GetTrackedByIdAndBusinessAsync(r.ProductId, r.BusinessId, t);
            if (p == null)
                return F(UpdateSaleItemErrors.ProductNotFound);
            if (!p.IsActive)
                return F(UpdateSaleItemErrors.ProductInactive);
            var c = await _categories.GetByIdAndBusinessAsync(p.CategoryId, r.BusinessId, t);
            if (c == null || !c.IsActive || c.Type != CategoryType.Sale)
                return F(UpdateSaleItemErrors.ProductCategoryUnavailable);
            _movements.SetOriginalVersions(m, r.MovementVersion, i, r.ItemVersion);
            i = m.UpdateSaleItem(
                r.ItemId,
                p,
                r.Quantity,
                r.UnitAmount,
                r.CurrentUserId,
                _clock.UtcNow
            );
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
                return F(UpdateSaleItemErrors.ConcurrencyConflict);
            }
            catch
            {
                return F(UpdateSaleItemErrors.OperationFailed);
            }
            var dto = new FinancialMovementItemResult(
                i.Id,
                i.ProductId.Value,
                i.Description,
                i.CategoryId,
                c.Name,
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
            return Result<FinancialMovementItemMaintenanceResult>.Success(
                new(dto, m.TotalAmount, m.Version)
            );
        }

        private static Result<FinancialMovementItemMaintenanceResult> F(ApplicationError e) =>
            Result<FinancialMovementItemMaintenanceResult>.Failure(e);
    }
}
