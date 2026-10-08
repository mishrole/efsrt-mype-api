using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Endpoints.v1.Auth;
using Mype.Application.Auth.Commands.RegisterUser;
using Mype.Application.Common;
using Mype.Domain.Users;
using Mype.Shared.Constants;
using Mype.Shared.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Api.Endpoints.v1.Auth
{
    public class RegisterUserEndpointTests
    {
        private const string DisplayName = "Usuario de prueba";
        private const string Email = "usuario@dominio.com";
        private const string Password = "password1";
        private const string TraceId = "test-trace-id";
        private const UserStatus Status = UserStatus.Active;

        private static readonly DateTimeOffset CreatedAt =
            new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

        private static RegisterUserEndpoint.RegisterUserRequest CreateRequest()
        {
            return new RegisterUserEndpoint.RegisterUserRequest(
                DisplayName,
                Email,
                Password,
                Password
            );
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            return new DefaultHttpContext
            {
                TraceIdentifier = TraceId
            };
        }

        private readonly Mock<ISender> _senderMock = new();

        [Fact]
        public async Task DoAsync_Should_Return_Created_When_Registration_Succeeds()
        {
            var userId = Guid.NewGuid();

            var applicationResult =
                Result<RegisterUserResult>.Success(
                    new RegisterUserResult
                    {
                        UserId = userId,
                        Email = Email,
                        DisplayName = DisplayName,
                        Status = Status,
                        CreatedAt = CreatedAt
                    }
                );

            _senderMock
                .Setup(sender => sender.Send(
                    It.IsAny<RegisterUserCommand>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(applicationResult);

            var request = CreateRequest();
            var context = CreateHttpContext();

            var result = await RegisterUserEndpoint.DoAsync(
                request,
                _senderMock.Object,
                context,
                CancellationToken.None
            );

            var statusResult = result
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Subject;

            statusResult.StatusCode.Should().Be(
                StatusCodes.Status201Created
            );

            var valueResult = result
                .Should()
                .BeAssignableTo<IValueHttpResult>()
                .Subject;

            var response = valueResult.Value
                .Should()
                .BeOfType<RegisterUserResult>()
                .Subject;

            response.UserId.Should().Be(userId);
            response.Email.Should().Be(Email);
            response.DisplayName.Should().Be(DisplayName);
            response.Status.Should().Be(Status);
            response.CreatedAt.Should().Be(CreatedAt);

            _senderMock.Verify(
                sender => sender.Send(
                    It.Is<RegisterUserCommand>(command =>
                        command.DisplayName == DisplayName &&
                        command.Email == Email &&
                        command.Password == Password &&
                        command.PasswordConfirmation == Password
                    ),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task DoAsync_Should_Return_Conflict_When_Email_Already_Exists()
        {
            var applicationResult =
                Result<RegisterUserResult>.Failure(
                    RegisterUserErrors.EmailAlreadyRegistered
                );

            _senderMock
                .Setup(sender => sender.Send(
                    It.IsAny<RegisterUserCommand>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(applicationResult);

            var request = CreateRequest();
            var context = CreateHttpContext();

            var result = await RegisterUserEndpoint.DoAsync(
                request,
                _senderMock.Object,
                context,
                CancellationToken.None
            );

            var statusResult = result
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Subject;

            statusResult.StatusCode.Should().Be(
                StatusCodes.Status409Conflict
            );

            var valueResult = result
                .Should()
                .BeAssignableTo<IValueHttpResult>()
                .Subject;

            var response = valueResult.Value
                .Should()
                .BeOfType<HttpStatusCodeInfo>()
                .Subject;

            response.Code.Should().Be(
                ErrorCodes.EmailAlreadyRegistered
            );

            response.Message.Should().Be(
                ErrorMessages.EmailAlreadyRegistered
            );

            response.StatusCode.Should().Be(
                StatusCodes.Status409Conflict
            );

            response.TraceId.Should().Be(TraceId);
        }
    }
}