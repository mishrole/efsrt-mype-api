using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.Auth.Commands.Login;
using Mype.Application.Auth.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Interfaces;
using Mype.Application.Users.Interfaces;
using Mype.Domain.AuthSessions;

namespace Mype.Application.Auth.Commands.RefreshSession
{
    public sealed class RefreshSessionCommandHandler
        : IRequestHandler<RefreshSessionCommand, Result<RefreshSessionResult>>
    {
        private readonly IAuthSessionRepository _sessionRepository;
        private readonly IRefreshTokenHelper _refreshTokenHelper;
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenHelper _jwtTokenHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public RefreshSessionCommandHandler(
            IAuthSessionRepository sessionRepository,
            IRefreshTokenHelper refreshTokenHelper,
            IUserRepository userRepository,
            IJwtTokenHelper jwtTokenHelper,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _sessionRepository = sessionRepository;
            _refreshTokenHelper = refreshTokenHelper;
            _userRepository = userRepository;
            _jwtTokenHelper = jwtTokenHelper;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<RefreshSessionResult>> Handle(
            RefreshSessionCommand request,
            CancellationToken cancellationToken
        )
        {
            var utcNow = _clock.UtcNow;
            var verifier = _refreshTokenHelper.ComputeVerifier(request.RefreshToken);
            var session = await _sessionRepository.GetByVerifierForUpdateAsync(
                verifier,
                cancellationToken
            );

            if (session == null)
            {
                return Result<RefreshSessionResult>.Failure(
                    RefreshSessionErrors.SessionUnavailable
                );
            }

            if (session.IsRotated())
            {
                await RevokeFamilyAsync(session.FamilyId, utcNow, cancellationToken);
                return Result<RefreshSessionResult>.Failure(
                    RefreshSessionErrors.SessionReuseDetected
                );
            }

            if (!session.IsActive(utcNow))
            {
                RevokeExpiredSession(session, utcNow);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<RefreshSessionResult>.Failure(
                    RefreshSessionErrors.SessionUnavailable
                );
            }

            var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
            if (user == null || !user.IsActive())
            {
                await RevokeUserSessionsAsync(session.UserId, utcNow, cancellationToken);
                return Result<RefreshSessionResult>.Failure(
                    RefreshSessionErrors.SessionUnavailable
                );
            }

            var refreshToken = _refreshTokenHelper.Generate(utcNow);
            var replacement = session.Rotate(
                refreshToken.Verifier,
                utcNow,
                refreshToken.IdleExpiresAt
            );
            await _sessionRepository.AddAsync(replacement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = _jwtTokenHelper.GenerateToken(
                user.Id,
                user.Email,
                user.DisplayName,
                replacement.Id,
                utcNow
            );

            return Result<RefreshSessionResult>.Success(
                new RefreshSessionResult
                {
                    AccessToken = accessToken.Value,
                    TokenType = accessToken.TokenType,
                    ExpiresAt = accessToken.ExpiresAt,
                    RefreshToken = refreshToken.Value,
                    RefreshTokenExpiresAt = replacement.IdleExpiresAt,
                    User = new AuthenticatedUserResult
                    {
                        Id = user.Id,
                        Email = user.Email,
                        DisplayName = user.DisplayName,
                        Status = user.Status,
                    },
                }
            );
        }

        private async Task RevokeFamilyAsync(
            Guid familyId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken
        )
        {
            var sessions = await _sessionRepository.ListActiveByFamilyIdAsync(
                familyId,
                utcNow,
                cancellationToken
            );

            foreach (var session in sessions)
            {
                session.Revoke(AuthSessionRevocationReason.ReuseDetected, utcNow);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task RevokeUserSessionsAsync(
            Guid userId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken
        )
        {
            var sessions = await _sessionRepository.ListActiveByUserIdAsync(
                userId,
                utcNow,
                cancellationToken
            );

            foreach (var session in sessions)
            {
                session.Revoke(AuthSessionRevocationReason.AccountDeactivated, utcNow);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static void RevokeExpiredSession(AuthSession session, DateTimeOffset utcNow)
        {
            var reason =
                utcNow >= session.AbsoluteExpiresAt
                    ? AuthSessionRevocationReason.AbsoluteExpired
                    : AuthSessionRevocationReason.IdleExpired;

            session.Revoke(reason, utcNow);
        }
    }
}
