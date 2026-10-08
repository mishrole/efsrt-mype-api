using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Domain.BusinessMemberships;
using Mype.Infrastructure.Persistence;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Infrastructure.BusinessMemberships.Repositories
{
    public class BusinessMembershipRepository : IBusinessMembershipRepository
    {
        private readonly MypeDbContext _dbContext;

        public BusinessMembershipRepository(
            MypeDbContext dbContext
        )
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            BusinessMembership membership,
            CancellationToken cancellationToken
        )
        {
            await _dbContext.BusinessMemberships.AddAsync(
                membership,
                cancellationToken
            );
        }
    }
}