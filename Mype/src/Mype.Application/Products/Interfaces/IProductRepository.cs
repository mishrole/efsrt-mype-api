using Mype.Application.Products.Models;
using Mype.Domain.Products;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Products.Interfaces
{
    public interface IProductRepository
    {
        Task AddAsync(
            Product product,
            CancellationToken cancellationToken
        );

        Task<bool>
            ExistsByBusinessAndNormalizedNameAsync(
                Guid businessId,
                string normalizedName,
                CancellationToken cancellationToken
            );

        Task<bool>
            ExistsOtherByBusinessAndNormalizedNameAsync(
                Guid businessId,
                string normalizedName,
                Guid excludedProductId,
                CancellationToken cancellationToken
            );

        Task<Product> GetTrackedByIdAndBusinessAsync(
            Guid productId,
            Guid businessId,
            CancellationToken cancellationToken
        );

        void SetOriginalVersion(
            Product product,
            uint version
        );

        Task<
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
        );

        Task<ProductDetailProjection>
            GetByIdAndBusinessAsync(
                Guid productId,
                Guid businessId,
                CancellationToken cancellationToken
            );
    }
}
