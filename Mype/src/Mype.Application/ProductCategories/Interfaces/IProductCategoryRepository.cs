using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mype.Domain.ProductCategories;

namespace Mype.Application.ProductCategories.Interfaces
{
    public interface IProductCategoryRepository
    {
        Task AddAsync(ProductCategory productCategory, CancellationToken cancellationToken);
        Task<ProductCategory> GetByIdAndBusinessAsync(
            Guid productCategoryId,
            Guid businessId,
            CancellationToken cancellationToken
        );
        Task<ProductCategory> GetTrackedByIdAndBusinessAsync(
            Guid productCategoryId,
            Guid businessId,
            CancellationToken cancellationToken
        );
        Task<ProductCategory> GetByNormalizedNameAsync(
            Guid businessId,
            string normalizedName,
            CancellationToken cancellationToken
        );
        Task<IReadOnlyCollection<ProductCategory>> ListAsync(
            Guid businessId,
            string normalizedSearch,
            bool? isActive,
            CancellationToken cancellationToken
        );
        void SetOriginalVersion(ProductCategory productCategory, uint version);
    }
}
