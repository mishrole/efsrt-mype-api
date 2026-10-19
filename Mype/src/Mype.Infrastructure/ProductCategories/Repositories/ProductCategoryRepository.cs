using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mype.Application.ProductCategories.Interfaces;
using Mype.Domain.ProductCategories;
using Mype.Infrastructure.Persistence;

namespace Mype.Infrastructure.ProductCategories.Repositories
{
    public sealed class ProductCategoryRepository : IProductCategoryRepository
    {
        private readonly MypeDbContext _dbContext;

        public ProductCategoryRepository(MypeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            ProductCategory productCategory,
            CancellationToken cancellationToken
        )
        {
            await _dbContext.ProductCategories.AddAsync(productCategory, cancellationToken);
        }

        public Task<ProductCategory> GetByIdAndBusinessAsync(
            Guid productCategoryId,
            Guid businessId,
            CancellationToken cancellationToken
        )
        {
            return _dbContext
                .ProductCategories.AsNoTracking()
                .SingleOrDefaultAsync(
                    productCategory =>
                        productCategory.Id == productCategoryId
                        && productCategory.BusinessId == businessId,
                    cancellationToken
                );
        }

        public Task<ProductCategory> GetTrackedByIdAndBusinessAsync(
            Guid productCategoryId,
            Guid businessId,
            CancellationToken cancellationToken
        )
        {
            return _dbContext.ProductCategories.SingleOrDefaultAsync(
                productCategory =>
                    productCategory.Id == productCategoryId
                    && productCategory.BusinessId == businessId,
                cancellationToken
            );
        }

        public Task<ProductCategory> GetByNormalizedNameAsync(
            Guid businessId,
            string normalizedName,
            CancellationToken cancellationToken
        )
        {
            return _dbContext
                .ProductCategories.AsNoTracking()
                .SingleOrDefaultAsync(
                    productCategory =>
                        productCategory.BusinessId == businessId
                        && productCategory.NormalizedName == normalizedName,
                    cancellationToken
                );
        }

        public async Task<IReadOnlyCollection<ProductCategory>> ListAsync(
            Guid businessId,
            string normalizedSearch,
            bool? isActive,
            CancellationToken cancellationToken
        )
        {
            var productCategoryQuery = _dbContext
                .ProductCategories.AsNoTracking()
                .Where(productCategory => productCategory.BusinessId == businessId);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                productCategoryQuery = productCategoryQuery.Where(productCategory =>
                    EF.Functions.ILike(productCategory.NormalizedName, $"%{normalizedSearch}%")
                );
            }
            if (isActive.HasValue)
            {
                productCategoryQuery = productCategoryQuery.Where(productCategory =>
                    productCategory.IsActive == isActive.Value
                );
            }
            return await productCategoryQuery
                .OrderBy(productCategory => productCategory.Name)
                .ToArrayAsync(cancellationToken);
        }

        public void SetOriginalVersion(ProductCategory productCategory, uint version)
        {
            _dbContext
                .Entry(productCategory)
                .Property(currentProductCategory => currentProductCategory.Version)
                .OriginalValue = version;
        }
    }
}
