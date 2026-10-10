using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mype.Application.Categories.Models;
using Mype.Domain.Categories;

namespace Mype.Application.Categories.Interfaces
{
    public interface ICategoryRepository
    {
        Task AddAsync(Category category, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<CategoryListItemProjection>> ListByBusinessAsync(
            Guid businessId,
            CategoryType? type,
            bool? isActive,
            CancellationToken cancellationToken
        );

        Task<Category> GetByIdAndBusinessAsync(
            Guid categoryId,
            Guid businessId,
            CancellationToken cancellationToken
        );

        Task<bool> ExistsByIdAndBusinessAsync(
            Guid categoryId,
            Guid businessId,
            CancellationToken cancellationToken
        );
    }
}
