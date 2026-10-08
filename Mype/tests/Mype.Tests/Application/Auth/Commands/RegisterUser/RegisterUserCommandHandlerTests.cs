using FluentAssertions;
using Moq;
using Mype.Application.Auth.Commands.RegisterUser;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.Users.Interfaces;
using Mype.Domain.Users;
using Mype.Shared.Constants;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Application.Auth.Commands.RegisterUser
{
    public class RegisterUserCommandHandlerTests
    {
        private const string Email = " user@example.com ";
        private const string TrimmedEmail = "user@example.com";
        private const string NormalizedEmail = "USER@EXAMPLE.COM";
        private const string Password = "password1";
        private const string PasswordHash = "password-hash";
        private const string DisplayName = "Test User";
        private const string Status = "ACTIVE";

        private static readonly DateTimeOffset UtcNow =
        new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPasswordHasherHelper> _passwordHasherMock = new();
        private readonly Mock<IEmailNormalizer> _emailNormalizerMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IClock> _clockMock = new();

        private readonly RegisterUserCommandHandler _handler;

        public RegisterUserCommandHandlerTests()
        {
            _emailNormalizerMock
            .Setup(normalizer => normalizer.Normalize(TrimmedEmail))
            .Returns(NormalizedEmail);

            _passwordHasherMock
            .Setup(hasher => hasher.HashPassword(Password))
            .Returns(PasswordHash);

            _clockMock
            .SetupGet(clock => clock.UtcNow)
            .Returns(UtcNow);

            _unitOfWorkMock
            .Setup(unitOfWork =>
            unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()
            )
            )
            .ReturnsAsync(1);

            _handler = new RegisterUserCommandHandler(
                _userRepositoryMock.Object,
                _passwordHasherMock.Object,
                _emailNormalizerMock.Object,
                _unitOfWorkMock.Object,
                _clockMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Register_User_When_Email_Is_Available()
        {
            var command = CreateCommand();

            _userRepositoryMock
            .Setup(repository =>
            repository.ExistsByNormalizedEmailAsync(
                NormalizedEmail,
                It.IsAny<CancellationToken>()
            )
            )
            .ReturnsAsync(false);

            var result = await _handler.Handle(
                command,
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Should().NotBeNull();
            result.Value.UserId.Should().NotBeEmpty();
            result.Value.Email.Should().Be(TrimmedEmail);
            result.Value.DisplayName.Should().Be(DisplayName);
            result.Value.Status.Should().Be(Status);
            result.Value.CreatedAt.Should().Be(UtcNow);

            _userRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<User>(user =>
                user.Email == TrimmedEmail &&
                user.NormalizedEmail == NormalizedEmail &&
                user.PasswordHash == PasswordHash &&
                user.DisplayName == DisplayName
            ),
            It.IsAny<CancellationToken>()
            ),
            Times.Once
            );

            _passwordHasherMock.Verify(
                hasher => hasher.HashPassword(Password),
                Times.Once
            );

            _unitOfWorkMock.Verify(
                unitOfWork =>
                unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()
            ),
            Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Normalize_Email_Before_Checking_Existence()
        {
            var command = CreateCommand();

            _userRepositoryMock
            .Setup(repository =>
            repository.ExistsByNormalizedEmailAsync(
                NormalizedEmail,
                It.IsAny<CancellationToken>()
            )
            )
            .ReturnsAsync(false);

            await _handler.Handle(
                command,
                CancellationToken.None
            );

            _emailNormalizerMock.Verify(
                normalizer => normalizer.Normalize(TrimmedEmail),
                Times.Once
            );

            _userRepositoryMock.Verify(
            repository =>
            repository.ExistsByNormalizedEmailAsync(
                NormalizedEmail,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Email_Already_Exists()
        {
            var command = CreateCommand();

            _userRepositoryMock
            .Setup(repository =>
                repository.ExistsByNormalizedEmailAsync(
                NormalizedEmail,
                It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

            var result = await _handler.Handle(
                command,
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();

            result.Error.Should().NotBeNull();

            result.Error.Code.Should().Be(
                ErrorCodes.EmailAlreadyRegistered
            );

            result.Error.Message.Should().Be(
                ErrorMessages.EmailAlreadyRegistered
            );

            result.Error.Type.Should().Be(
                ApplicationErrorType.Conflict
            );

            result.Value.Should().BeNull();

            _passwordHasherMock.Verify(
            hasher => hasher.HashPassword(
                It.IsAny<string>()
            ),
            Times.Never
            );

            _userRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
            );

            _unitOfWorkMock.Verify(unitOfWork =>
            unitOfWork.SaveChangesAsync(
                It.IsAny<CancellationToken>()
            ),
            Times.Never
            );
        }

        private static RegisterUserCommand CreateCommand()
        {
            return new RegisterUserCommand
            {
                DisplayName = DisplayName,
                Email = Email,
                Password = Password,
                PasswordConfirmation = Password
            };
        }
    }
}
