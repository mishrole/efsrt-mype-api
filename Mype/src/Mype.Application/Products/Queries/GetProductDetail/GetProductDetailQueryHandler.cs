using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.Products.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Permissions.Constants;

namespace Mype.Application.Products.Queries.GetProductDetail
{
    public sealed class GetProductDetailQueryHandler
        : IRequestHandler<GetProductDetailQuery, Result<ProductDetailResult>>
    {
        private readonly IBusinessMembershipRepository _membershipRepository;

        private readonly IPermissionRepository _permissionRepository;

        private readonly IProductRepository _productRepository;

        public GetProductDetailQueryHandler(
            IBusinessMembershipRepository membershipRepository,
            IPermissionRepository permissionRepository,
            IProductRepository productRepository
        )
        {
            _membershipRepository = membershipRepository;

            _permissionRepository = permissionRepository;

            _productRepository = productRepository;
        }

        public async Task<Result<ProductDetailResult>> Handle(
            GetProductDetailQuery request,
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
                return Failure(GetProductDetailErrors.BusinessAccessForbidden);
            }

            if (context.BusinessStatus != BusinessStatus.Active)
            {
                return Failure(GetProductDetailErrors.BusinessUnavailable);
            }

            var permissions = await _permissionRepository.ListActiveCodesByRoleIdAsync(
                context.RoleId,
                cancellationToken
            );

            if (!permissions.Contains(SystemPermissions.ProductRead.Code))
            {
                return Failure(GetProductDetailErrors.ProductAccessForbidden);
            }

            var product = await _productRepository.GetByIdAndBusinessAsync(
                request.ProductId,
                request.BusinessId,
                cancellationToken
            );

            if (product == null)
            {
                return Failure(GetProductDetailErrors.ProductNotFound);
            }

            return Result<ProductDetailResult>.Success(
                new ProductDetailResult(
                    product.Id,
                    product.BusinessId,
                    product.CategoryId,
                    product.CategoryName,
                    product.CategoryType,
                    product.Name,
                    product.SalePrice,
                    product.UnitCost,
                    product.IsActive,
                    product.CreatedAt,
                    product.UpdatedAt,
                    product.Version
                )
            );
        }

        private static Result<ProductDetailResult> Failure(ApplicationError error)
        {
            return Result<ProductDetailResult>.Failure(error);
        }
    }
}
