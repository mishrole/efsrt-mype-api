using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mype.Domain.AuthSessions;

namespace Mype.Application.Auth.Interfaces
{
    public interface IAuthSessionRepository
    {
        Task AddAsync(AuthSession session, CancellationToken cancellationToken);
        Task<AuthSession> GetByVerifierForUpdateAsync(
            string verifier,
            CancellationToken cancellationToken
        );
        Task<bool> IsActiveAsync(
            Guid sessionId,
            Guid userId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken
        );
        Task<IReadOnlyCollection<AuthSession>> ListActiveByFamilyIdAsync(
            Guid familyId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken
        );
        Task<IReadOnlyCollection<AuthSession>> ListActiveByUserIdAsync(
            Guid userId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken
        );
    }
}
