using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mype.Application.BusinessRoles.Interfaces;
using Mype.Domain.BusinessRoles;
using Mype.Infrastructure.Persistence;

namespace Mype.Infrastructure.BusinessRoles.Repositories
{
    public class BusinessRoleRepository : IBusinessRoleRepository
    {
        private readonly MypeDbContext _dbContext;

        public BusinessRoleRepository(MypeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<BusinessRole> GetActiveByCodeAsync(
            string code,
            CancellationToken cancellationToken
        )
        {
            return _dbContext
                .BusinessRoles.AsNoTracking()
                .SingleOrDefaultAsync(
                    role => role.Code == code && role.IsActive,
                    cancellationToken
                );
        }
    }
}
