using MediatR;
using Mype.Application.Common;
using Mype.Application.Common.Interfaces;
using Mype.Application.Users.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherHelper _passwordHasher;
        private readonly IEmailNormalizer _emailNormalizer;
        private readonly IJwtTokenHelper _jwtTokenHelper;
        private readonly IClock _clock;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IPasswordHasherHelper passwordHasher,
            IEmailNormalizer emailNormalizer,
            IJwtTokenHelper jwtTokenHelper,
            IClock clock
        )
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailNormalizer = emailNormalizer;
            _jwtTokenHelper = jwtTokenHelper;
            _clock = clock;
        }

        public async Task<Result<LoginResult>> Handle(
            LoginCommand request,
            CancellationToken cancellationToken
        )
        {
            var normalizedEmail = _emailNormalizer.Normalize(
                request.Email
            );

            var user = await _userRepository.GetByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken
            );

            if (user == null)
            {
                return Result<LoginResult>.Failure(
                    LoginErrors.InvalidCredentials
                );
            }

            var passwordIsValid = _passwordHasher.VerifyPassword(
                user.PasswordHash,
                request.Password
            );

            if (!passwordIsValid)
            {
                return Result<LoginResult>.Failure(
                    LoginErrors.InvalidCredentials
                );
            }

            if (!user.IsActive())
            {
                return Result<LoginResult>.Failure(
                    LoginErrors.AccountUnavailable
                );
            }

            var issuedAt = _clock.UtcNow;

            var token = _jwtTokenHelper.GenerateToken(
                user.Id,
                user.Email,
                user.DisplayName,
                issuedAt
            );

            return Result<LoginResult>.Success(
                new LoginResult
                {
                    AccessToken = token.Value,
                    TokenType = token.TokenType,
                    ExpiresAt = token.ExpiresAt,
                    User = new AuthenticatedUserResult
                    {
                        Id = user.Id,
                        Email = user.Email,
                        DisplayName = user.DisplayName,
                        Status = user.Status
                    }
                }
            );
        }
    }
}