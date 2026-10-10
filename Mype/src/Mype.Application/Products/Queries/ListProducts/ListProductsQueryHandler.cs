using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common;
using Mype.Application.Permissions.Interfaces;
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

        private readonly IProductRepository _productRepository;

        public ListProductsQueryHandler(
            IBusinessMembershipRepository membershipRepository,
            IPermissionRepository permissionRepository,
            ICategoryRepository categoryRepository,
            IProductRepository productRepository
        )
        {
            _membershipRepository = membershipRepository;

            _permissionRepository = permissionRepository;

            _categoryRepository = categoryRepository;

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

            var normalizedSearch = string.IsNullOrWhiteSpace(request.Search)
                ? null
                : ProductNameNormalizer.NormalizeForComparison(request.Search);

            var products = await _productRepository.ListByBusinessAsync(
                request.BusinessId,
                normalizedSearch,
                request.CategoryId,
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
