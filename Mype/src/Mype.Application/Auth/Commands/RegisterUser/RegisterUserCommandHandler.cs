using MediatR;
using Mype.Application.Common;
using Mype.Application.Common.Interfaces;
using Mype.Application.Users.Interfaces;
using Mype.Domain.Users;
using Mype.Shared.Constants;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Auth.Commands.RegisterUser
{
    public class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, Result<RegisterUserResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherHelper _passwordHasher;
        private readonly IEmailNormalizer _emailNormalizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public RegisterUserCommandHandler(
            IUserRepository userRepository,
            IPasswordHasherHelper passwordHasher,
            IEmailNormalizer emailNormalizer,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailNormalizer = emailNormalizer;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<RegisterUserResult>> Handle(
            RegisterUserCommand request,
            CancellationToken cancellationToken
        )
        {
            var email = request.Email.Trim();
            var normalizedEmail = _emailNormalizer.Normalize(email);

            var emailExists =
            await _userRepository.ExistsByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken
            );

            if (emailExists)
            {
                return Result<RegisterUserResult>.Failure(
                    ErrorCodes.EmailAlreadyRegistered
                );
            }

            var passwordHash = _passwordHasher.HashPassword(
                request.Password
            );

            var user = User.Create(
                email,
                normalizedEmail,
                passwordHash,
                request.DisplayName.Trim(),
                _clock.UtcNow
            );

            await _userRepository.AddAsync(
                user,
                cancellationToken
            );

            await _unitOfWork.SaveChangesAsync(
                cancellationToken
            );

            return Result<RegisterUserResult>.Success(
                new RegisterUserResult
                {
                    UserId = user.Id,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    Status = user.Status,
                    CreatedAt = user.CreatedAt
                }
            );
        }
    }
}
