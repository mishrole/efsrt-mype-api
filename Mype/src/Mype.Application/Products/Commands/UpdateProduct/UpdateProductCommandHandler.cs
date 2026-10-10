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
using Mype.Application.Products.Normalizers;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Application.Products.Commands.UpdateProduct
{
    public sealed class UpdateProductCommandHandler
        : IRequestHandler<UpdateProductCommand, Result<ProductMaintenanceResult>>
    {
        private readonly IBusinessMembershipRepository _membershipRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public UpdateProductCommandHandler(
            IBusinessMembershipRepository membershipRepository,
            IPermissionRepository permissionRepository,
            ICategoryRepository categoryRepository,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _membershipRepository = membershipRepository;
            _permissionRepository = permissionRepository;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<ProductMaintenanceResult>> Handle(
            UpdateProductCommand request,
            CancellationToken cancellationToken
        )
        {
            var context = await _membershipRepository.GetContextByBusinessAndUserAsync(
                request.BusinessId,
                request.CurrentUserId,
                cancellationToken
            );
            if (
                context == null
                || context.MembershipStatus != BusinessMembershipStatus.Active
                || !context.RoleIsActive
            )
                return Failure(UpdateProductErrors.BusinessAccessForbidden);
            if (context.BusinessStatus != BusinessStatus.Active)
                return Failure(UpdateProductErrors.BusinessUnavailable);

            var permissions = await _permissionRepository.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );
            if (!permissions.Contains(SystemPermissions.ProductUpdate.Code))
                return Failure(UpdateProductErrors.ProductAccessForbidden);

            var product = await _productRepository.GetTrackedByIdAndBusinessAsync(
                request.ProductId,
                request.BusinessId,
                cancellationToken
            );
            if (product == null)
                return Failure(UpdateProductErrors.ProductNotFound);

            var category = await _categoryRepository.GetByIdAndBusinessAsync(
                request.CategoryId,
                request.BusinessId,
                cancellationToken
            );
            if (category == null)
                return Failure(UpdateProductErrors.CategoryNotFound);
            if (category.Type != CategoryType.Sale)
                return Failure(UpdateProductErrors.ProductCategoryMustBeSale);
            if (!category.IsActive)
                return Failure(UpdateProductErrors.CategoryUnavailable);

            var name = ProductNameNormalizer.NormalizeName(request.Name);
            var normalizedName = ProductNameNormalizer.NormalizeForComparison(request.Name);
            if (
                await _productRepository.ExistsOtherByBusinessAndNormalizedNameAsync(
                    request.BusinessId,
                    normalizedName,
                    request.ProductId,
                    cancellationToken
                )
            )
                return Failure(UpdateProductErrors.ProductAlreadyExists);

            _productRepository.SetOriginalVersion(product, request.Version);
            product.Update(
                request.CategoryId,
                name,
                normalizedName,
                request.SalePrice,
                request.UnitCost,
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
                when (exception.Code == ErrorCodes.ProductAlreadyExists)
            {
                return Failure(UpdateProductErrors.ProductAlreadyExists);
            }
            catch (ApplicationErrorException exception)
                when (exception.Code == ErrorCodes.ProductConcurrencyConflict)
            {
                return Failure(UpdateProductErrors.ProductConcurrencyConflict);
            }
            catch
            {
                return Failure(UpdateProductErrors.ProductUpdateFailed);
            }

            return Result<ProductMaintenanceResult>.Success(ToResult(product, category.Name));
        }

        private static ProductMaintenanceResult ToResult(
            Mype.Domain.Products.Product product,
            string categoryName
        ) =>
            new(
                product.Id,
                product.BusinessId,
                product.CategoryId,
                categoryName,
                product.Name,
                product.SalePrice,
                product.UnitCost,
                product.IsActive,
                product.DeactivatedAt,
                product.CreatedAt,
                product.UpdatedAt,
                product.Version
            );

        private static Result<ProductMaintenanceResult> Failure(ApplicationError error) =>
            Result<ProductMaintenanceResult>.Failure(error);
    }
}
