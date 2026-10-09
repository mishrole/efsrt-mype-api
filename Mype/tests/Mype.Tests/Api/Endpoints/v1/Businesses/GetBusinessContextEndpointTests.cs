using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.Businesses;
using Mype.Application.Businesses.Queries.GetBusinessContext;
using Mype.Application.Common;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;
using Mype.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Api.Endpoints.v1.Businesses
{
    public class GetBusinessContextEndpointTests
    {
        private const string DisplayName =
            "Bodega Central";

        private const string CurrencyCode =
            "PEN";

        private const string RoleCode =
            "OWNER";

        private const string TraceId =
            "test-trace-id";

        private static readonly Guid CurrentUserId =
            Guid.NewGuid();

        private static readonly Guid BusinessId =
            Guid.NewGuid();

        private static readonly Guid MembershipId =
            Guid.NewGuid();

        private static readonly Guid RoleId =
            Guid.NewGuid();

        private readonly Mock<ISender> _senderMock =
            new();

        private readonly Mock<IUserContextProvider>
            _userContextProviderMock = new();

        public GetBusinessContextEndpointTests()
        {
            _userContextProviderMock
                .Setup(provider =>
                    provider.GetCurrentUserId()
                )
                .Returns(CurrentUserId);
        }

        [Fact]
        public async Task DoAsync_Should_Return_Ok_With_Business_Context()
        {
            SetupSender(
                Result<
                    BusinessContextResult
                >.Success(
                    CreateApplicationResult()
                )
            );

            var result =
                await GetBusinessContextEndpoint.DoAsync(
                    BusinessId,
                    _senderMock.Object,
                    _userContextProviderMock.Object,
                    CreateHttpContext(),
                    CancellationToken.None
                );

            var statusResult = result
                .Should()
                .BeAssignableTo<
                    IStatusCodeHttpResult
                >()
                .Subject;

            statusResult.StatusCode.Should().Be(
                StatusCodes.Status200OK
            );

            var valueResult = result
                .Should()
                .BeAssignableTo<
                    IValueHttpResult
                >()
                .Subject;

            var response = valueResult.Value
                .Should()
                .BeOfType<
                    BusinessContextResult
                >()
                .Subject;

            response.BusinessId.Should().Be(
                BusinessId
            );

            response.DisplayName.Should().Be(
                DisplayName
            );

            response.CurrencyCode.Should().Be(
                CurrencyCode
            );

            response.MembershipId.Should().Be(
                MembershipId
            );

            response.RoleId.Should().Be(
                RoleId
            );

            response.RoleCode.Should().Be(
                RoleCode
            );

            response.Permissions.Should()
                .HaveCount(2);

            response.Permissions.Should().Contain(
                SystemPermissions
                    .BusinessRead.Code
            );

            response.Permissions.Should().Contain(
                SystemPermissions
                    .DashboardRead.Code
            );

            VerifyQueryWasSent();
        }

        [Fact]
        public async Task DoAsync_Should_Return_Forbidden_When_Access_Is_Denied()
        {
            SetupSender(
                Result<
                    BusinessContextResult
                >.Failure(
                    GetBusinessContextErrors
                        .BusinessAccessForbidden
                )
            );

            var result =
                await GetBusinessContextEndpoint.DoAsync(
                    BusinessId,
                    _senderMock.Object,
                    _userContextProviderMock.Object,
                    CreateHttpContext(),
                    CancellationToken.None
                );

            AssertErrorResponse(
                result,
                StatusCodes.Status403Forbidden,
                ErrorCodes.BusinessAccessForbidden,
                ErrorMessages
                    .BusinessAccessForbidden
            );

            VerifyQueryWasSent();
        }

        [Fact]
        public async Task DoAsync_Should_Return_Conflict_When_Business_Is_Unavailable()
        {
            SetupSender(
                Result<
                    BusinessContextResult
                >.Failure(
                    GetBusinessContextErrors
                        .BusinessUnavailable
                )
            );

            var result =
                await GetBusinessContextEndpoint.DoAsync(
                    BusinessId,
                    _senderMock.Object,
                    _userContextProviderMock.Object,
                    CreateHttpContext(),
                    CancellationToken.None
                );

            AssertErrorResponse(
                result,
                StatusCodes.Status409Conflict,
                ErrorCodes.BusinessUnavailable,
                ErrorMessages.BusinessUnavailable
            );

            VerifyQueryWasSent();
        }

        [Fact]
        public async Task DoAsync_Should_Create_Query_With_Business_And_Current_User()
        {
            SetupSender(
                Result<
                    BusinessContextResult
                >.Success(
                    CreateApplicationResult()
                )
            );

            await GetBusinessContextEndpoint.DoAsync(
                BusinessId,
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            _userContextProviderMock.Verify(
                provider =>
                    provider.GetCurrentUserId(),
                Times.Once
            );

            VerifyQueryWasSent();
        }

        [Fact]
        public async Task DoAsync_Should_Forward_CancellationToken()
        {
            using var cancellationTokenSource =
                new CancellationTokenSource();

            var cancellationToken =
                cancellationTokenSource.Token;

            SetupSender(
                Result<
                    BusinessContextResult
                >.Success(
                    CreateApplicationResult()
                ),
                cancellationToken
            );

            await GetBusinessContextEndpoint.DoAsync(
                BusinessId,
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                cancellationToken
            );

            _senderMock.Verify(
                sender => sender.Send(
                    It.IsAny<
                        GetBusinessContextQuery
                    >(),
                    cancellationToken
                ),
                Times.Once
            );
        }

        private void SetupSender(
            Result<BusinessContextResult> result,
            CancellationToken? cancellationToken =
                null
        )
        {
            _senderMock
                .Setup(sender => sender.Send(
                    It.IsAny<
                        GetBusinessContextQuery
                    >(),
                    cancellationToken.HasValue
                        ? cancellationToken.Value
                        : It.IsAny<
                            CancellationToken
                        >()
                ))
                .ReturnsAsync(result);
        }

        private void VerifyQueryWasSent()
        {
            _senderMock.Verify(
                sender => sender.Send(
                    It.Is<
                        GetBusinessContextQuery
                    >(
                        query =>
                            query.BusinessId ==
                                BusinessId &&
                            query.CurrentUserId ==
                                CurrentUserId
                    ),
                    It.IsAny<
                        CancellationToken
                    >()
                ),
                Times.Once
            );
        }

        private static void AssertErrorResponse(
            IResult result,
            int expectedStatusCode,
            string expectedCode,
            string expectedMessage
        )
        {
            var statusResult = result
                .Should()
                .BeAssignableTo<
                    IStatusCodeHttpResult
                >()
                .Subject;

            statusResult.StatusCode.Should().Be(
                expectedStatusCode
            );

            var valueResult = result
                .Should()
                .BeAssignableTo<
                    IValueHttpResult
                >()
                .Subject;

            var response = valueResult.Value
                .Should()
                .BeOfType<HttpStatusCodeInfo>()
                .Subject;

            response.Code.Should().Be(
                expectedCode
            );

            response.StatusCode.Should().Be(
                expectedStatusCode
            );

            response.Message.Should().Be(
                expectedMessage
            );

            response.TraceId.Should().Be(
                TraceId
            );
        }

        private static BusinessContextResult
            CreateApplicationResult()
        {
            IReadOnlyCollection<string>
                permissions =
                [
                    SystemPermissions
                        .BusinessRead.Code,
                    SystemPermissions
                        .DashboardRead.Code
                ];

            return new BusinessContextResult(
                BusinessId,
                DisplayName,
                CurrencyCode,
                MembershipId,
                RoleId,
                RoleCode,
                permissions
            );
        }

        private static DefaultHttpContext
            CreateHttpContext()
        {
            return new DefaultHttpContext
            {
                TraceIdentifier = TraceId
            };
        }
    }
}