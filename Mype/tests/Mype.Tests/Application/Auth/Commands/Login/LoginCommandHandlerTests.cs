using FluentAssertions;
using Moq;
using Mype.Application.Auth.Commands.Login;
using Mype.Application.Common.Interfaces;
using Mype.Application.Common.Models;
using Mype.Application.Users.Interfaces;
using Mype.Domain.Users;
using Mype.Shared.Constants;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Application.Auth.Commands.Login
{
    public class LoginCommandHandlerTests
    {
        private const string Email = "  user@example.com  ";
        private const string OriginalEmail = "user@example.com";
        private const string NormalizedEmail = "USER@EXAMPLE.COM";
        private const string Password = "password1";
        private const string InvalidPassword = "invalid-password";
        private const string PasswordHash = "password-hash";
        private const string DisplayName = "Test User";
        private const string AccessToken = "access-token";
        private const string TokenType = AuthConstants.TokenType;

        private static readonly DateTimeOffset CreatedAt =
            new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

        private static readonly DateTimeOffset IssuedAt =
            CreatedAt.AddHours(1);

        private static readonly DateTimeOffset ExpiresAt =
            IssuedAt.AddMinutes(60);

        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPasswordHasherHelper> _passwordHasherMock = new();
        private readonly Mock<IEmailNormalizer> _emailNormalizerMock = new();
        private readonly Mock<IJwtTokenHelper> _jwtTokenHelperMock = new();
        private readonly Mock<IClock> _clockMock = new();

        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _emailNormalizerMock
                .Setup(normalizer => normalizer.Normalize(Email))
                .Returns(NormalizedEmail);

            _clockMock
                .SetupGet(clock => clock.UtcNow)
                .Returns(IssuedAt);

            _jwtTokenHelperMock
                .Setup(helper => helper.GenerateToken(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTimeOffset>()
                ))
                .Returns(new JwtTokenResult
                {
                    Value = AccessToken,
                    TokenType = TokenType,
                    IssuedAt = IssuedAt,
                    ExpiresAt = ExpiresAt
                });

            _handler = new LoginCommandHandler(
                _userRepositoryMock.Object,
                _passwordHasherMock.Object,
                _emailNormalizerMock.Object,
                _jwtTokenHelperMock.Object,
                _clockMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Authenticate_Active_User_When_Credentials_Are_Valid()
        {
            var user = CreateUser();
            var command = CreateCommand();

            SetupUserLookup(user);
            SetupPasswordVerification(Password, true);

            var result = await _handler.Handle(
                command,
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Should().NotBeNull();

            result.Value.AccessToken.Should().Be(AccessToken);
            result.Value.TokenType.Should().Be(TokenType);
            result.Value.ExpiresAt.Should().Be(ExpiresAt);

            result.Value.User.Id.Should().Be(user.Id);
            result.Value.User.Email.Should().Be(OriginalEmail);
            result.Value.User.DisplayName.Should().Be(DisplayName);
            result.Value.User.Status.Should().Be(UserStatus.Active);

            _passwordHasherMock.Verify(
                hasher => hasher.VerifyPassword(
                    PasswordHash,
                    Password
                ),
                Times.Once
            );

            _jwtTokenHelperMock.Verify(
                helper => helper.GenerateToken(
                    user.Id,
                    OriginalEmail,
                    DisplayName,
                    IssuedAt
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Normalize_Email_Before_Looking_Up_User()
        {
            var user = CreateUser();
            var command = CreateCommand();

            SetupUserLookup(user);
            SetupPasswordVerification(Password, true);

            await _handler.Handle(
                command,
                CancellationToken.None
            );

            _emailNormalizerMock.Verify(
                normalizer => normalizer.Normalize(Email),
                Times.Once
            );

            _userRepositoryMock.Verify(
                repository => repository.GetByNormalizedEmailAsync(
                    NormalizedEmail,
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_InvalidCredentials_When_User_Does_Not_Exist()
        {
            var command = CreateCommand();

            SetupUserLookup(null);

            var result = await _handler.Handle(
                command,
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.Error.Should().BeSameAs(
                LoginErrors.InvalidCredentials
            );

            result.Error.Code.Should().Be(
                ErrorCodes.InvalidCredentials
            );

            _passwordHasherMock.Verify(
                hasher => hasher.VerifyPassword(
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
                Times.Never
            );

            _jwtTokenHelperMock.Verify(
                helper => helper.GenerateToken(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTimeOffset>()
                ),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Return_InvalidCredentials_When_Password_Is_Incorrect()
        {
            var user = CreateUser();

            var command = CreateCommand(
                InvalidPassword
            );

            SetupUserLookup(user);
            SetupPasswordVerification(InvalidPassword, false);

            var result = await _handler.Handle(
                command,
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.Error.Should().BeSameAs(
                LoginErrors.InvalidCredentials
            );

            result.Error.Code.Should().Be(
                ErrorCodes.InvalidCredentials
            );

            _jwtTokenHelperMock.Verify(
                helper => helper.GenerateToken(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTimeOffset>()
                ),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Return_AccountUnavailable_When_User_Is_Inactive()
        {
            var user = CreateUser();

            user.Deactivate(
                CreatedAt.AddMinutes(30)
            );

            var command = CreateCommand();

            SetupUserLookup(user);
            SetupPasswordVerification(Password, true);

            var result = await _handler.Handle(
                command,
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.Error.Should().BeSameAs(
                LoginErrors.AccountUnavailable
            );

            result.Error.Code.Should().Be(
                ErrorCodes.AccountUnavailable
            );

            _jwtTokenHelperMock.Verify(
                helper => helper.GenerateToken(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTimeOffset>()
                ),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Authenticate_When_Email_Is_Not_Verified()
        {
            var user = CreateUser();
            var command = CreateCommand();

            user.EmailVerified.Should().BeFalse();

            SetupUserLookup(user);
            SetupPasswordVerification(Password, true);

            var result = await _handler.Handle(
                command,
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();

            _jwtTokenHelperMock.Verify(
                helper => helper.GenerateToken(
                    user.Id,
                    OriginalEmail,
                    DisplayName,
                    IssuedAt
                ),
                Times.Once
            );
        }

        private void SetupUserLookup(User user)
        {
            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByNormalizedEmailAsync(
                        NormalizedEmail,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(user);
        }

        private void SetupPasswordVerification(
            string providedPassword,
            bool isValid
        )
        {
            _passwordHasherMock
                .Setup(hasher => hasher.VerifyPassword(
                    PasswordHash,
                    providedPassword
                ))
                .Returns(isValid);
        }

        private static LoginCommand CreateCommand(
            string password = Password
        )
        {
            return new LoginCommand
            {
                Email = Email,
                Password = password
            };
        }

        private static User CreateUser()
        {
            return User.Create(
                OriginalEmail,
                NormalizedEmail,
                PasswordHash,
                DisplayName,
                CreatedAt
            );
        }
    }
}