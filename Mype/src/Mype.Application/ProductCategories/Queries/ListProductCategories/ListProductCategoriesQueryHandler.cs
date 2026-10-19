using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.ProductCategories.Interfaces;

namespace Mype.Application.ProductCategories.Queries.ListProductCategories
{
    public sealed class ListProductCategoriesQueryHandler
        : IRequestHandler<
            ListProductCategoriesQuery,
            Result<IReadOnlyCollection<ProductCategoryResult>>
        >
    {
        readonly IBusinessMembershipRepository m;
        readonly IPermissionRepository p;
        readonly IProductCategoryRepository r;

        public ListProductCategoriesQueryHandler(
            IBusinessMembershipRepository m,
            IPermissionRepository p,
            IProductCategoryRepository r
        )
        {
            this.m = m;
            this.p = p;
            this.r = r;
        }

        public async Task<Result<IReadOnlyCollection<ProductCategoryResult>>> Handle(
            ListProductCategoriesQuery q,
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
                return Result<IReadOnlyCollection<ProductCategoryResult>>.Failure(e);
            var search = string.IsNullOrWhiteSpace(q.Search)
                ? null
                : ProductCategoryNameNormalizer.NormalizeForComparison(q.Search);
            var values = await r.ListAsync(q.BusinessId, search, q.IsActive, t);
            return Result<IReadOnlyCollection<ProductCategoryResult>>.Success(
                values.Select(ProductCategoryAccess.Map).ToArray()
            );
        }
    }
}
