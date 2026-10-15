using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mype.Application.Auth.Interfaces;
using Mype.Domain.AuthSessions;
using Mype.Infrastructure.Persistence;

namespace Mype.Infrastructure.Auth.Repositories
{
    public sealed class AuthSessionRepository : IAuthSessionRepository
    {
        private readonly MypeDbContext _dbContext;

        public AuthSessionRepository(MypeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(AuthSession session, CancellationToken cancellationToken)
        {
            await _dbContext.AuthSessions.AddAsync(session, cancellationToken);
        }

        public Task<AuthSession> GetByVerifierForUpdateAsync(
            string verifier,
            CancellationToken cancellationToken
        )
        {
            return _dbContext.AuthSessions.SingleOrDefaultAsync(
                session => session.RefreshTokenVerifier == verifier,
                cancellationToken
            );
        }

        public Task<bool> IsActiveAsync(
            Guid sessionId,
            Guid userId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken
        )
        {
            return _dbContext
                .AuthSessions.AsNoTracking()
                .AnyAsync(
                    session =>
                        session.Id == sessionId
                        && session.UserId == userId
                        && !session.RevokedAt.HasValue
                        && utcNow < session.IdleExpiresAt
                        && utcNow < session.AbsoluteExpiresAt,
                    cancellationToken
                );
        }

        public async Task<IReadOnlyCollection<AuthSession>> ListActiveByFamilyIdAsync(
            Guid familyId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken
        )
        {
            return await _dbContext
                .AuthSessions.Where(session =>
                    session.FamilyId == familyId
                    && !session.RevokedAt.HasValue
                    && utcNow < session.IdleExpiresAt
                    && utcNow < session.AbsoluteExpiresAt
                )
                .ToArrayAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<AuthSession>> ListActiveByUserIdAsync(
            Guid userId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken
        )
        {
            return await _dbContext
                .AuthSessions.Where(session =>
                    session.UserId == userId
                    && !session.RevokedAt.HasValue
                    && utcNow < session.IdleExpiresAt
                    && utcNow < session.AbsoluteExpiresAt
                )
                .ToArrayAsync(cancellationToken);
        }
    }
}
