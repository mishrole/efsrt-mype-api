using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.Auth.Commands.RefreshSession;
using Mype.Application.Auth.Interfaces;
using Mype.Application.Auth.Models;
using Mype.Application.Common.Interfaces;
using Mype.Application.Common.Models;
using Mype.Application.Users.Interfaces;
using Mype.Domain.AuthSessions;
using Mype.Domain.Users;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.Auth.Commands.RefreshSession
{
    public class RefreshSessionCommandHandlerTests
    {
        private const string RefreshToken = "refresh-token";
        private const string Verifier = "VERIFIER";
        private const string ReplacementToken = "replacement-token";
        private const string ReplacementVerifier = "REPLACEMENT_VERIFIER";
        private const string AccessToken = "access-token";
        private const string Email = "user@example.com";
        private const string NormalizedEmail = "USER@EXAMPLE.COM";
        private const string PasswordHash = "password-hash";
        private const string DisplayName = "Test User";

        private static readonly DateTimeOffset UtcNow = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

        private readonly Mock<IAuthSessionRepository> _sessionRepositoryMock = new();
        private readonly Mock<IRefreshTokenHelper> _refreshTokenHelperMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IJwtTokenHelper> _jwtTokenHelperMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IClock> _clockMock = new();
        private readonly RefreshSessionCommandHandler _handler;

        public RefreshSessionCommandHandlerTests()
        {
            _clockMock.SetupGet(clock => clock.UtcNow).Returns(UtcNow);
            _refreshTokenHelperMock
                .Setup(helper => helper.ComputeVerifier(RefreshToken))
                .Returns(Verifier);
            _refreshTokenHelperMock
                .Setup(helper => helper.Generate(UtcNow))
                .Returns(
                    new RefreshTokenResult(
                        ReplacementToken,
                        ReplacementVerifier,
                        UtcNow.AddDays(7),
                        UtcNow.AddDays(30)
                    )
                );
            _unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _jwtTokenHelperMock
                .Setup(helper =>
                    helper.GenerateToken(
                        It.IsAny<Guid>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Guid>(),
                        UtcNow
                    )
                )
                .Returns(
                    new JwtTokenResult
                    {
                        Value = AccessToken,
                        TokenType = AuthConstants.TokenType,
                        IssuedAt = UtcNow,
                        ExpiresAt = UtcNow.AddMinutes(60),
                    }
                );

            _handler = new RefreshSessionCommandHandler(
                _sessionRepositoryMock.Object,
                _refreshTokenHelperMock.Object,
                _userRepositoryMock.Object,
                _jwtTokenHelperMock.Object,
                _unitOfWorkMock.Object,
                _clockMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return_SessionUnavailable_When_Token_Is_Unknown()
        {
            SetupSessionLookup(null);

            var result = await HandleAsync();

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeSameAs(RefreshSessionErrors.SessionUnavailable);
            VerifyNoReplacementWasCreated();
        }

        [Fact]
        public async Task Handle_Should_Rotate_Session_And_Return_New_Credentials()
        {
            var user = CreateUser();
            var session = CreateSession(user.Id);
            AuthSession replacement = null;
            SetupSessionLookup(session);
            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(user);
            _sessionRepositoryMock
                .Setup(repository =>
                    repository.AddAsync(It.IsAny<AuthSession>(), It.IsAny<CancellationToken>())
                )
                .Callback<AuthSession, CancellationToken>((created, _) => replacement = created)
                .Returns(Task.CompletedTask);

            var result = await HandleAsync();

            result.IsSuccess.Should().BeTrue();
            result.Value.AccessToken.Should().Be(AccessToken);
            result.Value.TokenType.Should().Be(AuthConstants.TokenType);
            result.Value.RefreshToken.Should().Be(ReplacementToken);
            result.Value.RefreshTokenExpiresAt.Should().Be(UtcNow.AddDays(7));
            result.Value.User.Id.Should().Be(user.Id);

            session.RevocationReason.Should().Be(AuthSessionRevocationReason.Rotated);
            replacement.Should().NotBeNull();
            replacement.FamilyId.Should().Be(session.FamilyId);
            replacement.RefreshTokenVerifier.Should().Be(ReplacementVerifier);
            session.ReplacedBySessionId.Should().Be(replacement.Id);

            _jwtTokenHelperMock.Verify(
                helper => helper.GenerateToken(user.Id, Email, DisplayName, replacement.Id, UtcNow),
                Times.Once
            );
            _unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Revoke_Family_When_Rotated_Token_Is_Reused()
        {
            var rotatedSession = CreateSession();

            var activeReplacement = rotatedSession.Rotate(
                ReplacementVerifier,
                UtcNow.AddMinutes(-1),
                UtcNow.AddDays(AuthConstants.RefreshTokenIdleExpirationDays)
            );

            SetupSessionLookup(rotatedSession);

            _sessionRepositoryMock
                .Setup(repository =>
                    repository.ListActiveByFamilyIdAsync(
                        rotatedSession.FamilyId,
                        UtcNow,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(new[] { activeReplacement });

            var result = await HandleAsync();

            result.IsSuccess.Should().BeFalse();

            result.Error.Should().BeSameAs(RefreshSessionErrors.SessionReuseDetected);

            rotatedSession.IsRotated().Should().BeTrue();

            activeReplacement
                .RevocationReason.Should()
                .Be(AuthSessionRevocationReason.ReuseDetected);

            activeReplacement.RevokedAt.Should().Be(UtcNow);

            _unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once
            );

            VerifyNoReplacementWasCreated();
        }

        [Fact]
        public async Task Handle_Should_Revoke_Idle_Expired_Session()
        {
            var session = AuthSession.Create(
                Guid.NewGuid(),
                Verifier,
                UtcNow.AddDays(-8),
                UtcNow.AddMinutes(-1),
                UtcNow.AddDays(22)
            );
            SetupSessionLookup(session);

            var result = await HandleAsync();

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeSameAs(RefreshSessionErrors.SessionUnavailable);
            session.RevocationReason.Should().Be(AuthSessionRevocationReason.IdleExpired);
        }

        [Fact]
        public async Task Handle_Should_Revoke_Absolute_Expired_Session()
        {
            var session = AuthSession.Create(
                Guid.NewGuid(),
                Verifier,
                UtcNow.AddDays(-31),
                UtcNow.AddDays(1),
                UtcNow.AddMinutes(-1)
            );
            SetupSessionLookup(session);

            var result = await HandleAsync();

            result.IsSuccess.Should().BeFalse();
            session.RevocationReason.Should().Be(AuthSessionRevocationReason.AbsoluteExpired);
        }

        [Fact]
        public async Task Handle_Should_Revoke_User_Sessions_When_User_Is_Inactive()
        {
            var session = CreateSession();
            var anotherSession = CreateSession();
            var user = CreateUser();
            user.Deactivate(UtcNow.AddMinutes(-1));
            SetupSessionLookup(session);
            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(session.UserId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(user);
            _sessionRepositoryMock
                .Setup(repository =>
                    repository.ListActiveByUserIdAsync(
                        session.UserId,
                        UtcNow,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(new[] { session, anotherSession });

            var result = await HandleAsync();

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeSameAs(RefreshSessionErrors.SessionUnavailable);
            session.RevocationReason.Should().Be(AuthSessionRevocationReason.AccountDeactivated);
            anotherSession
                .RevocationReason.Should()
                .Be(AuthSessionRevocationReason.AccountDeactivated);
        }

        private Task<Mype.Application.Common.Result<RefreshSessionResult>> HandleAsync()
        {
            return _handler.Handle(
                new RefreshSessionCommand { RefreshToken = RefreshToken },
                CancellationToken.None
            );
        }

        private void SetupSessionLookup(AuthSession session)
        {
            _sessionRepositoryMock
                .Setup(repository =>
                    repository.GetByVerifierForUpdateAsync(Verifier, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(session);
        }

        private void VerifyNoReplacementWasCreated()
        {
            _sessionRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(It.IsAny<AuthSession>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
            _jwtTokenHelperMock.Verify(
                helper =>
                    helper.GenerateToken(
                        It.IsAny<Guid>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Guid>(),
                        It.IsAny<DateTimeOffset>()
                    ),
                Times.Never
            );
        }

        private static AuthSession CreateSession(Guid? userId = null)
        {
            return AuthSession.Create(
                userId ?? UserId,
                Verifier,
                UtcNow.AddDays(-1),
                UtcNow.AddDays(6),
                UtcNow.AddDays(29)
            );
        }

        private static readonly Guid UserId = Guid.NewGuid();

        private static User CreateUser()
        {
            return User.Create(
                Email,
                NormalizedEmail,
                PasswordHash,
                DisplayName,
                UtcNow.AddDays(-10)
            );
        }
    }
}
