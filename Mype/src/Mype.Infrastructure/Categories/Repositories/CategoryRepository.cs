using Mype.Application.Categories.Interfaces;
using Mype.Domain.Categories;
using Mype.Infrastructure.Persistence;
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
    }
}