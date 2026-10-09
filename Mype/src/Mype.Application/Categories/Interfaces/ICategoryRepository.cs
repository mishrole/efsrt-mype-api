using Mype.Application.Categories.Models;
using Mype.Domain.Categories;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Categories.Interfaces
{
    public interface ICategoryRepository
    {
        Task AddAsync(
            Category category,
            CancellationToken cancellationToken
        );

        Task<IReadOnlyCollection<CategoryListItemProjection>> ListByBusinessAsync(
            Guid businessId,
            CategoryType? type,
            bool? isActive,
            CancellationToken cancellationToken
        );
    }
}