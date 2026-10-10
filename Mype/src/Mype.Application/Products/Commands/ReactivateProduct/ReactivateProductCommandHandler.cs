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
using Mype.Application.Permissions.Interfaces;
using Mype.Application.Products.Commands.Common;
using Mype.Application.Products.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.Products.Commands.ReactivateProduct
{
    public sealed class ReactivateProductCommandHandler
        : IRequestHandler<ReactivateProductCommand, Result<ProductMaintenanceResult>>
    {
        private readonly IBusinessMembershipRepository _memberships;
        private readonly IPermissionRepository _permissions;
        private readonly IProductRepository _products;
        private readonly ICategoryRepository _categories;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public ReactivateProductCommandHandler(
            IBusinessMembershipRepository memberships,
            IPermissionRepository permissions,
            IProductRepository products,
            ICategoryRepository categories,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _memberships = memberships;
            _permissions = permissions;
            _products = products;
            _categories = categories;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<ProductMaintenanceResult>> Handle(
            ReactivateProductCommand request,
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
                return Failure(ReactivateProductErrors.BusinessAccessForbidden);
            if (context.BusinessStatus != BusinessStatus.Active)
                return Failure(ReactivateProductErrors.BusinessUnavailable);
            var permissions = await _permissions.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );
            if (!permissions.Contains(SystemPermissions.ProductReactivate.Code))
                return Failure(ReactivateProductErrors.ProductAccessForbidden);
            var product = await _products.GetTrackedByIdAndBusinessAsync(
                request.ProductId,
                request.BusinessId,
                cancellationToken
            );
            if (product == null)
                return Failure(ReactivateProductErrors.ProductNotFound);
            if (product.IsActive)
                return Failure(ReactivateProductErrors.ProductAlreadyActive);
            var category = await _categories.GetByIdAndBusinessAsync(
                product.CategoryId,
                request.BusinessId,
                cancellationToken
            );
            if (category == null || !category.IsActive || category.Type != CategoryType.Sale)
                return Failure(ReactivateProductErrors.ProductCategoryUnavailable);
            _products.SetOriginalVersion(product, request.Version);
            product.Reactivate(request.CurrentUserId, _clock.UtcNow);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ApplicationErrorException ex)
                when (ex.Code == ErrorCodes.ProductConcurrencyConflict)
            {
                return Failure(ReactivateProductErrors.ProductConcurrencyConflict);
            }
            catch
            {
                return Failure(ReactivateProductErrors.ProductStatusChangeFailed);
            }
            return Result<ProductMaintenanceResult>.Success(
                new(
                    product.Id,
                    product.BusinessId,
                    product.CategoryId,
                    category.Name,
                    product.Name,
                    product.SalePrice,
                    product.UnitCost,
                    product.IsActive,
                    product.DeactivatedAt,
                    product.CreatedAt,
                    product.UpdatedAt,
                    product.Version
                )
            );
        }

        private static Result<ProductMaintenanceResult> Failure(ApplicationError error) =>
            Result<ProductMaintenanceResult>.Failure(error);
    }
}
