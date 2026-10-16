using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.Auth.Commands.Logout;
using Mype.Application.Auth.Interfaces;
using Mype.Application.Common.Interfaces;
using Mype.Domain.AuthSessions;

namespace Mype.Tests.Application.Auth.Commands.Logout
{
    public class LogoutCommandHandlerTests
    {
        private const string RefreshToken = "refresh-token";
        private const string Verifier = "VERIFIER";
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly DateTimeOffset UtcNow = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

        private readonly Mock<IAuthSessionRepository> _sessionRepositoryMock = new();
        private readonly Mock<IRefreshTokenHelper> _refreshTokenHelperMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IClock> _clockMock = new();
        private readonly LogoutCommandHandler _handler;

        public LogoutCommandHandlerTests()
        {
            _refreshTokenHelperMock
                .Setup(helper => helper.ComputeVerifier(RefreshToken))
                .Returns(Verifier);
            _clockMock.SetupGet(clock => clock.UtcNow).Returns(UtcNow);
            _unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _handler = new LogoutCommandHandler(
                _sessionRepositoryMock.Object,
                _refreshTokenHelperMock.Object,
                _unitOfWorkMock.Object,
                _clockMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return_Success_When_Refresh_Token_Is_Empty()
        {
            var result = await _handler.Handle(new LogoutCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeSameAs(LogoutResult.Completed);
            _sessionRepositoryMock.VerifyNoOtherCalls();
            _unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Return_Success_When_Session_Does_Not_Exist()
        {
            _sessionRepositoryMock
                .Setup(repository =>
                    repository.GetByVerifierForUpdateAsync(Verifier, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync((AuthSession)null);

            var result = await _handler.Handle(
                new LogoutCommand { RefreshToken = RefreshToken },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            _unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Revoke_Current_Session()
        {
            var session = CreateSession();
            _sessionRepositoryMock
                .Setup(repository =>
                    repository.GetByVerifierForUpdateAsync(Verifier, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(session);

            var result = await _handler.Handle(
                new LogoutCommand { RefreshToken = RefreshToken },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            session.RevokedAt.Should().Be(UtcNow);
            session.RevocationReason.Should().Be(AuthSessionRevocationReason.Logout);
            _unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Not_Save_When_Session_Is_Already_Revoked()
        {
            var session = CreateSession();
            session.Revoke(AuthSessionRevocationReason.Logout, UtcNow.AddMinutes(-1));
            _sessionRepositoryMock
                .Setup(repository =>
                    repository.GetByVerifierForUpdateAsync(Verifier, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(session);

            var result = await _handler.Handle(
                new LogoutCommand { RefreshToken = RefreshToken },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            _unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        private static AuthSession CreateSession()
        {
            return AuthSession.Create(
                UserId,
                Verifier,
                UtcNow.AddDays(-1),
                UtcNow.AddDays(6),
                UtcNow.AddDays(29)
            );
        }
    }
}
