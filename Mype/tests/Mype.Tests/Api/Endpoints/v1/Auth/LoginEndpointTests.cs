using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Endpoints.v1.Auth;
using Mype.Application.Auth.Commands.Login;
using Mype.Application.Common;
using Mype.Domain.Users;
using Mype.Shared.Constants;
using Mype.Shared.Models;

namespace Mype.Tests.Api.Endpoints.v1.Auth
{
    public class LoginEndpointTests
    {
        private const string Email = "user@example.com";
        private const string Password = "password1";
        private const string AccessToken = "access-token";
        private const string TraceId = "test-trace-id";
        private const string TokenType = AuthConstants.TokenType;

        private static readonly Guid UserId = Guid.NewGuid();

        private static readonly DateTimeOffset ExpiresAt = new(
            2026,
            10,
            8,
            13,
            0,
            0,
            TimeSpan.Zero
        );

        private readonly Mock<ISender> _senderMock = new();

        [Fact]
        public async Task DoAsync_Should_Return_Ok_When_Login_Succeeds()
        {
            var applicationResult = Result<LoginResult>.Success(
                new LoginResult
                {
                    AccessToken = AccessToken,
                    TokenType = TokenType,
                    ExpiresAt = ExpiresAt,
                    User = new AuthenticatedUserResult
                    {
                        Id = UserId,
                        Email = Email,
                        DisplayName = "Test User",
                        Status = UserStatus.Active,
                    },
                }
            );

            _senderMock
                .Setup(sender =>
                    sender.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(applicationResult);

            var context = CreateHttpContext();

            var result = await LoginEndpoint.DoAsync(
                CreateRequest(),
                _senderMock.Object,
                context,
                CancellationToken.None
            );

            var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

            statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

            var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

            var response = valueResult.Value.Should().BeOfType<LoginResult>().Subject;

            response.AccessToken.Should().Be(AccessToken);

            response.TokenType.Should().Be(TokenType);

            response.ExpiresAt.Should().Be(ExpiresAt);
            response.User.Id.Should().Be(UserId);
            response.User.Status.Should().Be(UserStatus.Active);

            context.Response.Headers.CacheControl.ToString().Should().Be("no-store");

            context.Response.Headers.Pragma.ToString().Should().Be("no-cache");

            _senderMock.Verify(
                sender =>
                    sender.Send(
                        It.Is<LoginCommand>(command =>
                            command.Email == Email && command.Password == Password
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task DoAsync_Should_Return_Unauthorized_When_Credentials_Are_Invalid()
        {
            var applicationResult = Result<LoginResult>.Failure(LoginErrors.InvalidCredentials);

            _senderMock
                .Setup(sender =>
                    sender.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(applicationResult);

            var context = CreateHttpContext();

            var result = await LoginEndpoint.DoAsync(
                CreateRequest(),
                _senderMock.Object,
                context,
                CancellationToken.None
            );

            var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

            statusResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

            var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

            var response = valueResult.Value.Should().BeOfType<HttpStatusCodeInfo>().Subject;

            response.Code.Should().Be(ErrorCodes.InvalidCredentials);

            response.Message.Should().Be(ErrorMessages.InvalidCredentials);

            response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

            response.TraceId.Should().Be(TraceId);
        }

        [Fact]
        public async Task DoAsync_Should_Return_Forbidden_When_Account_Is_Unavailable()
        {
            var applicationResult = Result<LoginResult>.Failure(LoginErrors.AccountUnavailable);

            _senderMock
                .Setup(sender =>
                    sender.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(applicationResult);

            var context = CreateHttpContext();

            var result = await LoginEndpoint.DoAsync(
                CreateRequest(),
                _senderMock.Object,
                context,
                CancellationToken.None
            );

            var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

            statusResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

            var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

            var response = valueResult.Value.Should().BeOfType<HttpStatusCodeInfo>().Subject;

            response.Code.Should().Be(ErrorCodes.AccountUnavailable);

            response.Message.Should().Be(ErrorMessages.AccountUnavailable);

            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        private static LoginEndpoint.LoginRequest CreateRequest()
        {
            return new LoginEndpoint.LoginRequest(Email, Password);
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            return new DefaultHttpContext { TraceIdentifier = TraceId };
        }
    }
}
