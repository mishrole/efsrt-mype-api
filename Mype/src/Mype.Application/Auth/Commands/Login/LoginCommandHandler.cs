using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.Auth.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Interfaces;
using Mype.Application.Users.Interfaces;
using Mype.Domain.AuthSessions;

namespace Mype.Application.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherHelper _passwordHasher;
        private readonly IEmailNormalizer _emailNormalizer;
        private readonly IJwtTokenHelper _jwtTokenHelper;
        private readonly IRefreshTokenHelper _refreshTokenHelper;
        private readonly IAuthSessionRepository _sessionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IPasswordHasherHelper passwordHasher,
            IEmailNormalizer emailNormalizer,
            IJwtTokenHelper jwtTokenHelper,
            IRefreshTokenHelper refreshTokenHelper,
            IAuthSessionRepository sessionRepository,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailNormalizer = emailNormalizer;
            _jwtTokenHelper = jwtTokenHelper;
            _refreshTokenHelper = refreshTokenHelper;
            _sessionRepository = sessionRepository;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<LoginResult>> Handle(
            LoginCommand request,
            CancellationToken cancellationToken
        )
        {
            var normalizedEmail = _emailNormalizer.Normalize(request.Email);

            var user = await _userRepository.GetByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken
            );

            if (user == null)
            {
                return Result<LoginResult>.Failure(LoginErrors.InvalidCredentials);
            }

            var passwordIsValid = _passwordHasher.VerifyPassword(
                user.PasswordHash,
                request.Password
            );

            if (!passwordIsValid)
            {
                return Result<LoginResult>.Failure(LoginErrors.InvalidCredentials);
            }

            if (!user.IsActive())
            {
                return Result<LoginResult>.Failure(LoginErrors.AccountUnavailable);
            }

            var issuedAt = _clock.UtcNow;

            var refreshToken = _refreshTokenHelper.Generate(issuedAt);

            var session = AuthSession.Create(
                user.Id,
                refreshToken.Verifier,
                issuedAt,
                refreshToken.IdleExpiresAt,
                refreshToken.AbsoluteExpiresAt
            );

            await _sessionRepository.AddAsync(session, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var accessToken = _jwtTokenHelper.GenerateToken(
                user.Id,
                user.Email,
                user.DisplayName,
                session.Id,
                issuedAt
            );

            return Result<LoginResult>.Success(
                new LoginResult
                {
                    AccessToken = accessToken.Value,
                    TokenType = accessToken.TokenType,
                    ExpiresAt = accessToken.ExpiresAt,
                    RefreshToken = refreshToken.Value,
                    RefreshTokenExpiresAt = session.IdleExpiresAt,
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
    }
}
