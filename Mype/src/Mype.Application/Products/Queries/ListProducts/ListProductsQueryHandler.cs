using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.ProductCategories.Interfaces;
using Mype.Application.Products.Interfaces;
using Mype.Application.Products.Normalizers;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Permissions.Constants;

namespace Mype.Application.Products.Queries.ListProducts
{
    public sealed class ListProductsQueryHandler
        : IRequestHandler<ListProductsQuery, Result<IReadOnlyCollection<ProductListItemResult>>>
    {
        private readonly IBusinessMembershipRepository _membershipRepository;

        private readonly IPermissionRepository _permissionRepository;

        private readonly ICategoryRepository _categoryRepository;

        private readonly IProductCategoryRepository _productCategoryRepository;

        private readonly IProductRepository _productRepository;

        public ListProductsQueryHandler(
            IBusinessMembershipRepository membershipRepository,
            IPermissionRepository permissionRepository,
            ICategoryRepository categoryRepository,
            IProductCategoryRepository productCategoryRepository,
            IProductRepository productRepository
        )
        {
            _membershipRepository = membershipRepository;
            _permissionRepository = permissionRepository;
            _categoryRepository = categoryRepository;
            _productCategoryRepository = productCategoryRepository;
            _productRepository = productRepository;
        }

        public async Task<Result<IReadOnlyCollection<ProductListItemResult>>> Handle(
            ListProductsQuery request,
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
            {
                return Failure(ListProductsErrors.BusinessAccessForbidden);
            }

            if (context.BusinessStatus != BusinessStatus.Active)
            {
                return Failure(ListProductsErrors.BusinessUnavailable);
            }

            var permissions = await _permissionRepository.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );

            if (!permissions.Contains(SystemPermissions.ProductRead.Code))
            {
                return Failure(ListProductsErrors.ProductAccessForbidden);
            }

            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _categoryRepository.ExistsByIdAndBusinessAsync(
                    request.CategoryId.Value,
                    request.BusinessId,
                    cancellationToken
                );

                if (!categoryExists)
                {
                    return Failure(ListProductsErrors.CategoryNotFound);
                }
            }

            if (request.ProductCategoryId.HasValue)
            {
                var productCategory = await _productCategoryRepository.GetByIdAndBusinessAsync(
                    request.ProductCategoryId.Value,
                    request.BusinessId,
                    cancellationToken
                );

                if (productCategory is null)
                {
                    return Failure(ListProductsErrors.ProductCategoryNotFound);
                }
            }

            var normalizedSearch = string.IsNullOrWhiteSpace(request.Search)
                ? null
                : ProductNameNormalizer.NormalizeForComparison(request.Search);

            var products = await _productRepository.ListByBusinessAsync(
                request.BusinessId,
                normalizedSearch,
                request.CategoryId,
                request.ProductCategoryId,
                request.IsActive,
                request.AvailableForSale,
                cancellationToken
            );

            IReadOnlyCollection<ProductListItemResult> result = products
                .Select(product => new ProductListItemResult(
                    product.Id,
                    product.BusinessId,
                    product.CategoryId,
                    product.CategoryName,
                    product.ProductCategoryId,
                    product.ProductCategoryName,
                    product.ProductCategoryColor,
                    product.Name,
                    product.SalePrice,
                    product.UnitCost,
                    product.IsActive
                ))
                .ToArray();

            return Result<IReadOnlyCollection<ProductListItemResult>>.Success(result);
        }

        private static Result<IReadOnlyCollection<ProductListItemResult>> Failure(
            ApplicationError error
        )
        {
            return Result<IReadOnlyCollection<ProductListItemResult>>.Failure(error);
        }
    }
}
