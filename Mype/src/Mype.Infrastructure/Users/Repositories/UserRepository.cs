using Microsoft.EntityFrameworkCore;
using Mype.Application.Users.Interfaces;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Infrastructure.Users.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MypeDbContext _dbContext;

        public UserRepository(MypeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<bool> ExistsByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken
        )
        {
            return _dbContext.Users.AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken
            );
        }

        public Task<User> GetByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken
        )
        {
            return _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user => user.NormalizedEmail == normalizedEmail,
                    cancellationToken
                );
        }

        public async Task AddAsync(
            User user,
            CancellationToken cancellationToken
        )
        {
            await _dbContext.Users.AddAsync(
                user,
                cancellationToken
            );
        }

        public Task<User> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken
        )
        {
            return _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user => user.Id == id,
                    cancellationToken
                );
        }
    }
}
