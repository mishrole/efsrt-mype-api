using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.Businesses;
using Mype.Application.Businesses.Queries.ListUserBusinesses;
using Mype.Application.Common;
using Mype.Domain.Businesses;

namespace Mype.Tests.Api.Endpoints.v1.Businesses
{
    public class ListUserBusinessesEndpointTests
    {
        private const string DisplayName = "Bodega Central";

        private const string CurrencyCode = "PEN";

        private const string RoleCode = "OWNER";

        private static readonly Guid CurrentUserId = Guid.NewGuid();

        private static readonly Guid BusinessId = Guid.NewGuid();

        private static readonly Guid MembershipId = Guid.NewGuid();

        private readonly Mock<ISender> _senderMock = new();

        private readonly Mock<IUserContextProvider> _userContextProviderMock = new();

        public ListUserBusinessesEndpointTests()
        {
            _userContextProviderMock
                .Setup(provider => provider.GetCurrentUserId())
                .Returns(CurrentUserId);
        }

        [Fact]
        public async Task DoAsync_Should_Return_Ok_With_User_Businesses()
        {
            SetupSender(
                Result<IReadOnlyCollection<BusinessSummaryResult>>.Success(CreateBusinesses())
            );

            var result = await ListUserBusinessesEndpoint.DoAsync(
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

            statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

            var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

            var response = valueResult
                .Value.Should()
                .BeAssignableTo<IReadOnlyCollection<BusinessSummaryResult>>()
                .Subject;

            response.Should().ContainSingle();

            var business = response.Single();

            business.BusinessId.Should().Be(BusinessId);

            business.DisplayName.Should().Be(DisplayName);

            business.CurrencyCode.Should().Be(CurrencyCode);

            business.Status.Should().Be(BusinessStatus.Active);

            business.MembershipId.Should().Be(MembershipId);

            business.RoleCode.Should().Be(RoleCode);

            VerifyQueryWasSent();
        }

        [Fact]
        public async Task DoAsync_Should_Return_Ok_With_Empty_Collection()
        {
            SetupSender(
                Result<IReadOnlyCollection<BusinessSummaryResult>>.Success(
                    Array.Empty<BusinessSummaryResult>()
                )
            );

            var result = await ListUserBusinessesEndpoint.DoAsync(
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

            statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

            var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

            var response = valueResult
                .Value.Should()
                .BeAssignableTo<IReadOnlyCollection<BusinessSummaryResult>>()
                .Subject;

            response.Should().BeEmpty();

            VerifyQueryWasSent();
        }

        [Fact]
        public async Task DoAsync_Should_Create_Query_With_Current_User()
        {
            SetupSender(
                Result<IReadOnlyCollection<BusinessSummaryResult>>.Success(
                    Array.Empty<BusinessSummaryResult>()
                )
            );

            await ListUserBusinessesEndpoint.DoAsync(
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            _userContextProviderMock.Verify(provider => provider.GetCurrentUserId(), Times.Once);

            VerifyQueryWasSent();
        }

        [Fact]
        public async Task DoAsync_Should_Forward_CancellationToken()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            SetupSender(
                Result<IReadOnlyCollection<BusinessSummaryResult>>.Success(
                    Array.Empty<BusinessSummaryResult>()
                ),
                cancellationToken
            );

            await ListUserBusinessesEndpoint.DoAsync(
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                cancellationToken
            );

            _senderMock.Verify(
                sender => sender.Send(It.IsAny<ListUserBusinessesQuery>(), cancellationToken),
                Times.Once
            );
        }

        private void SetupSender(
            Result<IReadOnlyCollection<BusinessSummaryResult>> result,
            CancellationToken? cancellationToken = null
        )
        {
            _senderMock
                .Setup(sender =>
                    sender.Send(
                        It.IsAny<ListUserBusinessesQuery>(),
                        cancellationToken.HasValue
                            ? cancellationToken.Value
                            : It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(result);
        }

        private void VerifyQueryWasSent()
        {
            _senderMock.Verify(
                sender =>
                    sender.Send(
                        It.Is<ListUserBusinessesQuery>(query =>
                            query.CurrentUserId == CurrentUserId
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        private static IReadOnlyCollection<BusinessSummaryResult> CreateBusinesses()
        {
            return
            [
                new BusinessSummaryResult(
                    BusinessId,
                    DisplayName,
                    CurrencyCode,
                    BusinessStatus.Active,
                    MembershipId,
                    RoleCode
                ),
            ];
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            return new DefaultHttpContext();
        }
    }
}
