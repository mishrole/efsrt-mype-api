using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.ProductCategories.Interfaces;

namespace Mype.Application.ProductCategories.Queries.GetProductCategoryDetail
{
    public sealed class GetProductCategoryDetailQueryHandler
        : IRequestHandler<GetProductCategoryDetailQuery, Result<ProductCategoryResult>>
    {
        readonly IBusinessMembershipRepository m;
        readonly IPermissionRepository p;
        readonly IProductCategoryRepository r;

        public GetProductCategoryDetailQueryHandler(
            IBusinessMembershipRepository m,
            IPermissionRepository p,
            IProductCategoryRepository r
        )
        {
            this.m = m;
            this.p = p;
            this.r = r;
        }

        public async Task<Result<ProductCategoryResult>> Handle(
            GetProductCategoryDetailQuery q,
            CancellationToken t
        )
        {
            var e = await ProductCategoryAccess.ValidateAsync(
                m,
                p,
                q.BusinessId,
                q.CurrentUserId,
                ProductCategoryPermissions.Read,
                t
            );
            if (e != null)
                return Result<ProductCategoryResult>.Failure(e);
            var value = await r.GetByIdAndBusinessAsync(q.ProductCategoryId, q.BusinessId, t);
            return value == null
                ? Result<ProductCategoryResult>.Failure(ProductCategoryErrors.NotFound)
                : Result<ProductCategoryResult>.Success(ProductCategoryAccess.Map(value));
        }
    }
}
