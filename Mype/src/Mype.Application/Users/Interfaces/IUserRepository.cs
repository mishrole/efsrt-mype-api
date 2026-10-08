using Mype.Domain.Users;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Users.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistsByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken
        );

        Task<User> GetByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken
        );

        Task AddAsync(
            User user,
            CancellationToken cancellationToken
        );

        Task<User> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken
        );
    }
}
