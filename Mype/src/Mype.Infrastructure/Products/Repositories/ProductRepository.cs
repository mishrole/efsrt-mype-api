using Microsoft.EntityFrameworkCore;
using Mype.Application.Products.Interfaces;
using Mype.Application.Products.Models;
using Mype.Domain.Categories;
using Mype.Domain.Products;
using Mype.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Infrastructure.Products.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly MypeDbContext _dbContext;

        public ProductRepository(
            MypeDbContext dbContext
        )
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            Product product,
            CancellationToken cancellationToken
        )
        {
            await _dbContext.Products.AddAsync(
                product,
                cancellationToken
            );
        }

        public async Task<bool>
            ExistsByBusinessAndNormalizedNameAsync(
                Guid businessId,
                string normalizedName,
                CancellationToken cancellationToken
            )
        {
            return await _dbContext.Products
                .AsNoTracking()
                .AnyAsync(
                    product =>
                        product.BusinessId ==
                            businessId &&
                        product.NormalizedName ==
                            normalizedName,
                    cancellationToken
                );
        }

        public async Task<
            IReadOnlyCollection<
                ProductListItemProjection
            >
        > ListByBusinessAsync(
            Guid businessId,
            string normalizedSearch,
            Guid? categoryId,
            bool? isActive,
            bool availableForSale,
            CancellationToken cancellationToken
        )
        {
            var query =
                from product in
                    _dbContext.Products
                        .AsNoTracking()
                join category in
                    _dbContext.Categories
                        .AsNoTracking()
                    on new
                    {
                        product.CategoryId,
                        product.BusinessId
                    }
                    equals new
                    {
                        CategoryId =
                            category.Id,
                        category.BusinessId
                    }
                where
                    product.BusinessId ==
                        businessId
                select new
                {
                    Product = product,
                    Category = category
                };

            if (
                !string.IsNullOrWhiteSpace(
                    normalizedSearch
                )
            )
            {
                query = query.Where(item =>
                    EF.Functions.ILike(
                        item.Product.NormalizedName,
                        $"%{normalizedSearch}%"
                    )
                );
            }

            if (categoryId.HasValue)
            {
                query = query.Where(item =>
                    item.Product.CategoryId ==
                        categoryId.Value
                );
            }

            if (isActive.HasValue)
            {
                query = query.Where(item =>
                    item.Product.IsActive ==
                        isActive.Value
                );
            }

            if (availableForSale)
            {
                query = query.Where(item =>
                    item.Product.IsActive &&
                    item.Category.IsActive &&
                    item.Category.Type ==
                        CategoryType.Sale
                );
            }

            return await query
                .OrderBy(item =>
                    item.Product.Name
                )
                .Select(item =>
                    new ProductListItemProjection(
                        item.Product.Id,
                        item.Product.BusinessId,
                        item.Product.CategoryId,
                        item.Category.Name,
                        item.Product.Name,
                        item.Product.SalePrice,
                        item.Product.UnitCost,
                        item.Product.IsActive
                    )
                )
                .ToArrayAsync(cancellationToken);
        }

        public async Task<ProductDetailProjection>
            GetByIdAndBusinessAsync(
                Guid productId,
                Guid businessId,
                CancellationToken cancellationToken
            )
        {
            return await (
                from product in
                    _dbContext.Products
                        .AsNoTracking()
                join category in
                    _dbContext.Categories
                        .AsNoTracking()
                    on new
                    {
                        product.CategoryId,
                        product.BusinessId
                    }
                    equals new
                    {
                        CategoryId =
                            category.Id,
                        category.BusinessId
                    }
                where
                    product.Id == productId &&
                    product.BusinessId ==
                        businessId
                select new ProductDetailProjection(
                    product.Id,
                    product.BusinessId,
                    product.CategoryId,
                    category.Name,
                    category.Type,
                    product.Name,
                    product.SalePrice,
                    product.UnitCost,
                    product.IsActive,
                    product.CreatedAt,
                    product.UpdatedAt,
                    product.Version
                )
            ).SingleOrDefaultAsync(
                cancellationToken
            );
        }
    }
}