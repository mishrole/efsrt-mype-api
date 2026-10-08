using Mype.Application.Businesses.Interfaces;
using Mype.Domain.Businesses;
using Mype.Infrastructure.Persistence;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Infrastructure.Businesses.Repositories
{
    public class BusinessRepository : IBusinessRepository
    {
        private readonly MypeDbContext _dbContext;

        public BusinessRepository(
            MypeDbContext dbContext
        )
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            Business business,
            CancellationToken cancellationToken
        )
        {
            await _dbContext.Businesses.AddAsync(
                business,
                cancellationToken
            );
        }
    }
}