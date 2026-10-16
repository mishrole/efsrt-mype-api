using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.Auth.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Interfaces;
using Mype.Domain.AuthSessions;

namespace Mype.Application.Auth.Commands.Logout
{
    public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<LogoutResult>>
    {
        private readonly IAuthSessionRepository _sessionRepository;
        private readonly IRefreshTokenHelper _refreshTokenHelper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public LogoutCommandHandler(
            IAuthSessionRepository sessionRepository,
            IRefreshTokenHelper refreshTokenHelper,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _sessionRepository = sessionRepository;
            _refreshTokenHelper = refreshTokenHelper;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<LogoutResult>> Handle(
            LogoutCommand request,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Result<LogoutResult>.Success(LogoutResult.Completed);
            }

            var verifier = _refreshTokenHelper.ComputeVerifier(request.RefreshToken);
            var session = await _sessionRepository.GetByVerifierForUpdateAsync(
                verifier,
                cancellationToken
            );

            if (session == null || session.RevokedAt.HasValue)
            {
                return Result<LogoutResult>.Success(LogoutResult.Completed);
            }

            session.Revoke(AuthSessionRevocationReason.Logout, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<LogoutResult>.Success(LogoutResult.Completed);
        }
    }
}
