using Microsoft.EntityFrameworkCore;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Categories.Models;
using Mype.Domain.Categories;
using Mype.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Infrastructure.Categories.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly MypeDbContext _dbContext;

        public CategoryRepository(
            MypeDbContext dbContext
        )
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            Category category,
            CancellationToken cancellationToken
        )
        {
            await _dbContext.Categories.AddAsync(
                category,
                cancellationToken
            );
        }

        public async Task<
            IReadOnlyCollection<
                CategoryListItemProjection
            >
        > ListByBusinessAsync(
            Guid businessId,
            CategoryType? type,
            bool? isActive,
            CancellationToken cancellationToken
        )
        {
            var query =
                _dbContext.Categories
                    .AsNoTracking()
                    .Where(category =>
                        category.BusinessId ==
                        businessId
                    );

            if (type.HasValue)
            {
                query = query.Where(category =>
                    category.Type ==
                    type.Value
                );
            }

            if (isActive.HasValue)
            {
                query = query.Where(category =>
                    category.IsActive ==
                    isActive.Value
                );
            }

            return await query
                .OrderBy(category =>
                    category.Type
                )
                .ThenBy(category =>
                    category.Name
                )
                .Select(category =>
                    new CategoryListItemProjection(
                        category.Id,
                        category.BusinessId,
                        category.Name,
                        category.Type,
                        category.IsDefault,
                        category.IsActive
                    )
                )
                .ToArrayAsync(cancellationToken);
        }
    }
}